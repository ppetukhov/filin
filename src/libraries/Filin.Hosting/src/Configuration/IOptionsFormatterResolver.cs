namespace Filin.Configuration;

/// <summary>
/// IOptionFormatterResolver resolve specific OptionFormatter for certain named option.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
public interface IOptionsFormatterResolver<T>
{
    /// <summary>
    /// Resolves the options formatter for the specified options type with the specified options name.
    /// </summary>
    /// <param name="name">The options name.</param>
    /// <returns>The options type.</returns>
    IOptionsFormatter<T> Resolve(string? name);
}
