namespace Filin;

/// <summary>
/// Represents a base exception when an infrastructure or projection mapping failure occurs.
/// </summary>
public abstract class ProjectorException : FilinException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectorException"/> class.
    /// </summary>
    protected ProjectorException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectorException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected ProjectorException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectorException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected ProjectorException(string? message, Exception? innerException) : base(message, innerException) { }
}
