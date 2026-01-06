namespace Filin;

/// <summary>
/// Represents a configuration exception.
/// </summary>
public sealed class FilinConfigurationException : Exception
{
    /// <inheritdoc cref="FilinConfigurationException" />
    public FilinConfigurationException() : base() { }

    /// <inheritdoc cref="FilinConfigurationException" />
    public FilinConfigurationException(string? message) : base(message) { }

    /// <inheritdoc cref="FilinConfigurationException" />
    public FilinConfigurationException(string? message, Exception? innerException) : base(message, innerException) { }
}
