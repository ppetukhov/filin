namespace Filin;

/// <summary>
/// Represents an exception when a storage error occurs while retrieving the projection.
/// </summary>
public class ProjectionStoreException : FilinException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionStoreException"/> class.
    /// </summary>
    public ProjectionStoreException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionStoreException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProjectionStoreException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionStoreException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProjectionStoreException(string? message, Exception? innerException) : base(message, innerException) { }
}
