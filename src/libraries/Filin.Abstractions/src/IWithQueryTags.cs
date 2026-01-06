using System.Collections.Immutable;

namespace Filin;

/// <summary>
/// Объект с тэгами для формирования запроса.
/// </summary>
public interface IWithQueryTags
{
    /// <summary>
    /// Тэги для формирования запроса.
    /// </summary>
    ImmutableHashSet<Tag> GetTags();
}
