using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Represents the result of a command execution, containing either the updated state or a domain error.
/// </summary>
public readonly record struct ExecuteResult<TState> where TState : notnull
{
    readonly TState? _state;
    readonly Error _error;

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsSuccess => _state is not null;

    [MemberNotNullWhen(false, nameof(_state))]
    public bool IsFailure => !IsSuccess;

    public TState State => IsSuccess ? _state : throw new InvalidOperationException("Cannot access State of a failed command result.");
    public Error Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access Error of a successful command result.");

    ExecuteResult(TState state) => (_state, _error) = (state, default);
    ExecuteResult(Error error) => (_state, _error) = (default, error);

    public static ExecuteResult<TState> Success(TState state) => new(state);
    public static ExecuteResult<TState> Failure(Error error) => new(error);

    public static implicit operator ExecuteResult<TState>(TState state) => Success(state);
    public static implicit operator ExecuteResult<TState>(Error error) => Failure(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Match<TOut>(Func<TState, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_state) : onFailure(_error);
}
