using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Filin.Storage;

namespace Filin.Internal;

sealed class CommandHandler<TCommand, TState>(IDeciderProxy<TCommand, TState> decider, StateManager<TState> stateManager)
    where TCommand : notnull, ICommand<TState>
    where TState : notnull
{
    public async Task<ExecuteResult<TState>> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
    {
        var tags = command.GetTags();
        var query = stateManager.Projector.GetQuery(tags);
        var projectionId = stateManager.Projector.GetProjectionId(tags);

        while (true)
        {
            var (state, metadata) = await stateManager.LoadStateAsync(projectionId, query, cancellationToken).ConfigureAwait(false);

            var decideResult = state is null ? decider.Decide(command) : decider.Decide(command, state);

            if (decideResult.IsFailure)
            {
                return decideResult.Error;
            }

            AppendCondition appendCondition = new(query, metadata.Position);

            if (decideResult is { Count: 1 })
            {
                var @event = decideResult[0];
                var appendResult = await stateManager.EventStore.AppendAsync(@event, appendCondition, cancellationToken).ConfigureAwait(false);

                if (appendResult.IsConflict)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    continue;
                }

                var evt = appendResult[0];

                stateManager.ApplyEvent(ref state, ref metadata, ref evt);
            }
            else
            {
                var appendResult = await AppendAsync(decideResult, appendCondition, stateManager.EventStore, cancellationToken).ConfigureAwait(false);

                if (appendResult.IsConflict)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    continue;
                }

                foreach (var evt in appendResult)
                {
                    var @event = evt;

                    stateManager.ApplyEvent(ref state, ref metadata, ref @event);
                }

                Debug.Assert(state is not null);
            }

            stateManager.Save(state, projectionId, ref metadata);

            return state;
        }
    }

    static async Task<AppendResult> AppendAsync(DecideResult decideResult, AppendCondition appendCondition, IEventStore eventStore, CancellationToken cancellationToken)
    {
        DcbEvent[]? rentedBuffer = null;

        try
        {
            var events = GetDecideEvents(decideResult, out rentedBuffer);

            return await eventStore.AppendAsync(events, appendCondition, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                ArrayPool<DcbEvent>.Shared.Return(rentedBuffer);
            }
        }
    }

    static ArraySegment<DcbEvent> GetDecideEvents(DecideResult decideResult, [NotNull] out DcbEvent[] rentedBuff)
    {
        rentedBuff = ArrayPool<DcbEvent>.Shared.Rent(decideResult.Count);

        var idx = 0;

        foreach (var evt in decideResult)
        {
            rentedBuff[idx++] = evt;
        }

        return new ArraySegment<DcbEvent>(rentedBuff, 0, decideResult.Count);
    }
}
