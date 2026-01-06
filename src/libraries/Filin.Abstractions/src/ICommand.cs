namespace Filin;

/// <summary>
/// Команда (намериние) изменить состояние <typeparamref name="TState"/>.
/// </summary>
/// <remarks>
/// Команда выполняется по бизнес правилам на основе состояния <typeparamref name="TState"/>.
/// <see cref="ProjectorIdAttribute"/>
/// </remarks>
/// <typeparam name="TState">Тим модели данных состояния.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Идентификация команд должна выполняться во время компиляции.")]
public interface ICommand<TState> : IWithQueryTags where TState : notnull;
