namespace Filin;

/// <summary>
/// Represents an exception when a storage or operational error occurs in the event store.
/// </summary>
public class EventStoreException : FilinException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreException"/> class.
    /// </summary>
    public EventStoreException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EventStoreException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EventStoreException(string? message, Exception? innerException) : base(message, innerException) { }
}
