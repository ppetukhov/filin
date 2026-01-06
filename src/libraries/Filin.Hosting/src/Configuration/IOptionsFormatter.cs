namespace Filin.Configuration;

/// <summary>
/// Format the option and give it a category and a name.
/// </summary>
public interface IOptionsFormatter
{
    /// <summary>
    /// Gets the name of the options object.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Formats the options object into a collection of strings.
    /// </summary>
    void Format(Action<ReadOnlySpan<char>> writer);
}

/// <summary>
/// Option writer for a certain option type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
public interface IOptionsFormatter<T> : IOptionsFormatter
{
}
