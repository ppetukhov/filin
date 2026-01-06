namespace Filin;

/// <summary>
/// Оркестратор выполнения команд и запросов.
/// </summary>
public interface IDcbExecutor
{
    /// <summary>
    /// Выполнить команду на изменение состояния.
    /// </summary>
    /// <typeparam name="TCommand">Тип команды.</typeparam>
    /// <typeparam name="TState">Тип данных состояния.</typeparam>
    /// <param name="command"><inheritdoc cref="ICommand{TState}" path="/summary"/></param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <exception cref="DcbEventStoreException"/>
    /// <exception cref="DcbProjectorException"/>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <returns>Новое состояние <typeparamref name="TState"/>.</returns>
    Task<TState?> ExecuteAsync<TCommand, TState>(TCommand command, CancellationToken cancellationToken = default)
        where TState : notnull
        where TCommand : notnull, ICommand<TState>;

    /// <summary>
    /// Получить проекцию состояния.
    /// </summary>
    /// <typeparam name="TQuery">Типа запроса.</typeparam>
    /// <typeparam name="TProjection">Тип проекции состояния.</typeparam>
    /// <param name="query">Запрос на получение проекции.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <exception cref="DcbEventStoreException"/>
    /// <exception cref="DcbProjectorException"/>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <returns>Проекция типа <typeparamref name="TProjection"/>.</returns>
    Task<TProjection?> QueryAsync<TQuery, TProjection>(TQuery query, CancellationToken cancellationToken = default)
        where TProjection : notnull
        where TQuery : notnull, IQuery<TProjection>;
}
