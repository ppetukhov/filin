using System.Collections.Immutable;

namespace Filin;

/// <summary>
/// Проектор событий в модель данных для чтения и принятия решения пользователем или приложением.
/// </summary>
/// <remarks>
/// <see cref="ProjectorIdAttribute"/> используется для идентификации проектора.
/// <see cref="QueryEventTypesAttribute"/> и <see cref="QueryTagsAttribute"/> используется для задания фильтра проектора.
/// </remarks>
/// <typeparam name="TState">Тип объекта проекции.</typeparam>
public interface IProjector<TState> where TState : notnull
{
    /// <summary>
    /// Идентификатор проектора.
    /// </summary>
    static abstract ProjectorId ProjectorId { get; }

    /// <summary>
    /// Создать запрос на получение состояния проекции.
    /// </summary>
    /// <param name="tags">Коллекция тэгов из контекста.</param>
    /// <exception cref="ArgumentException">Отсутствует обязательный тэг.</exception>
    /// <returns></returns>
    static abstract DcbQuery GetQuery(ImmutableHashSet<Tag> tags);

    /// <summary>
    /// Создать идентификатор проекции.
    /// </summary>
    /// <param name="tags">Коллекция тэгов из контекста.</param>
    /// <exception cref="ArgumentException">Отсутствует обязательный тэг.</exception>
    /// <returns></returns>
    static abstract ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags);

    /// <summary>
    /// Создать проекцию.
    /// </summary>
    /// <param name="domainEvent">Доменное событие.</param>
    /// <exception cref="DcbProjectorException"/>
    /// <returns></returns>
    static abstract TState Project(DcbEventSequenced domainEvent);

    /// <summary>
    /// Изменить проекцию по событию.
    /// </summary>
    /// <param name="state">Текущая проекция.</param>
    /// <param name="domainEvent">Доменное событие.</param>
    /// <exception cref="DcbProjectorException"/>
    /// <returns></returns>
    static abstract TState Project(TState state, DcbEventSequenced domainEvent);
}
