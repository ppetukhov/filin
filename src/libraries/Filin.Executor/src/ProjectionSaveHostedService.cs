using System.Threading.Channels;

using Filin.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin;

sealed class ProjectionSaveHostedService(IServiceProvider services) : BackgroundService
{
    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var logger = services.GetRequiredService<ILogger<ProjectionSaveHostedService>>();

        logger.LogInformation($"{nameof(ProjectionSaveHostedService)} starting");

        _ = stoppingToken.Register(_ => logger.LogInformation($"{nameof(ProjectionSaveHostedService)} stopping"), null, false);

        var options = services.GetRequiredService<IOptions<ProjectionSaveOptions>>();

        var projectionStore = options.Value.ProjectionStore;

        var channels = CreateWorkChannels(count: options.Value.ConcurentWorks);

        var tasks = new List<Task>(capacity: options.Value.ConcurentWorks + 1);

        foreach (var channel in channels)
        {
            var reader = channel.Reader;

            tasks.Add(Task.Run(async () =>
            {
                await foreach (var (projection, create) in reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        await projectionStore.SaveAsync(projection, create, stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Failed to save projection");
                    }
                }
            }, cancellationToken: stoppingToken));
        }

        tasks.Add(ProcesingAsync(options.Value.Pipeline.Reader, channels, stoppingToken));

        return Task.WhenAll(tasks);
    }

    static List<Channel<ProjectionTuple>> CreateWorkChannels(int count) =>
        [.. Enumerable.Range(0, count).Select(static (_) =>
            Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<ProjectionTuple>
            {
                Comparer = ProjectionTupleComparer.Instance,
                SingleReader = true,
                SingleWriter = true,
            })
        )];

    static async Task ProcesingAsync(ChannelReader<ProjectionTuple> reader, List<Channel<ProjectionTuple>> channels, CancellationToken stoppingToken)
    {
        await foreach (var (projection, create) in reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            var idx = Math.Abs(projection.Id.GetHashCode()) % channels.Count;

            while (!stoppingToken.IsCancellationRequested && !channels[idx].Writer.TryWrite((projection, create)))
            {
                Thread.SpinWait(10);
            }
        }
    }

    sealed class ProjectionTupleComparer : IComparer<ProjectionTuple>
    {
        internal static readonly ProjectionTupleComparer Instance = new ProjectionTupleComparer();

        /// <inheritdoc/>
        public int Compare(ProjectionTuple x, ProjectionTuple y) =>
            x.create && !y.create ? 1 : !x.create && y.create ? -1 : x.projection.Position.CompareTo(y.projection.Position);
    }
}
