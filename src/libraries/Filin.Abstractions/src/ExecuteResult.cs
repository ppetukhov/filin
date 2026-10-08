using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Represents the result of a command execution, containing either the updated state or a domain rule error.
/// </summary>
/// <typeparam name="TState">The type of the state produced by a successful command execution.</typeparam>
public readonly record struct ExecuteResult<TState> where TState : notnull
{
    readonly TState? _state;
    readonly DomainRuleError _error;

    /// <summary>
    /// Gets a value indicating whether the command execution completed successfully.
    /// </summary>
    /// <value><see langword="true"/> if the result contains a valid state; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsSuccess => _state is not null;

    /// <summary>
    /// Gets a value indicating whether the command execution failed with a domain error.
    /// </summary>
    /// <value><see langword="true"/> if the result contains a domain error; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(false, nameof(_state))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the updated state produced by the successful command execution.
    /// </summary>
    /// <value>The instance of <typeparamref name="TState"/>.</value>
    /// <exception cref="InvalidOperationException">Thrown if the operation failed and there is no state to access.</exception>
    public TState State => IsSuccess ? _state : throw new InvalidOperationException("Cannot access State of a failed command result.");

    /// <summary>
    /// Gets the domain error that caused the command execution to fail.
    /// </summary>
    /// <value>The <see cref="Error"/> descriptor.</value>
    /// <exception cref="InvalidOperationException">Thrown if the operation succeeded and there is no error to access.</exception>
    public DomainRuleError Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access DomainRuleError of a successful command result.");

    internal ExecuteResult(TState state) => (_state, _error) = (state, default);
    internal ExecuteResult(DomainRuleError error) => (_state, _error) = (default, error);

    /// <summary>
    /// Executes the appropriate delegate based on whether the result indicates success or failure, returning a computed value.
    /// </summary>
    /// <typeparam name="TOut">The type of the output value produced by the projection delegates.</typeparam>
    /// <param name="onSuccess">The function to execute if the command succeeded, receiving the <typeparamref name="TState"/>.</param>
    /// <param name="onFailure">The function to execute if the command failed, receiving the <see cref="Error"/>.</param>
    /// <returns>The result computed by the invoked delegate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Match<TOut>(Func<TState, TOut> onSuccess, Func<DomainRuleError, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_state) : onFailure(_error);
    }
#pragma warning disable CA2225 // Operator overloads have named alternates
    /// <summary>
    /// Implicitly converts a state instance into a successful <see cref="ExecuteResult{TState}"/>.
    /// </summary>
    /// <param name="state">The successful state payload.</param>
    public static implicit operator ExecuteResult<TState>(TState state) => new(state);

    /// <summary>
    /// Implicitly converts an error instance into a failed <see cref="ExecuteResult{TState}"/>.
    /// </summary>
    /// <param name="error">The domain error payload.</param>
    public static implicit operator ExecuteResult<TState>(DomainRuleError error) => new(error);
#pragma warning restore CA2225 // Operator overloads have named alternates
}

/// <summary>
/// Represents the result of a command execution, containing either the updated state or a domain rule error.
/// </summary>
public static class ExecuteResult
{
    /// <summary>
    /// Creates a successful execution result containing the updated state.
    /// </summary>
    /// <param name="state">The state produced by the successful command execution.</param>
    /// <returns>A successful <see cref="ExecuteResult{TState}"/> instance.</returns>
    public static ExecuteResult<TState> Success<TState>(TState state)
         where TState : notnull => new(state);

    /// <summary>
    /// Creates a failed execution result containing the domain error.
    /// </summary>
    /// <param name="error">The domain error that caused the failure.</param>
    /// <returns>A failed <see cref="ExecuteResult{TState}"/> instance.</returns>
    public static ExecuteResult<TState> Failure<TState>(DomainRuleError error)
         where TState : notnull
        => new(error);
}
