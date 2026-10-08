using Filin;

namespace Edu.Exceptions;

/// <summary>
/// The exception that is thrown when a projector encounters an event type it is not configured to handle.
/// </summary>
public sealed class UnexpectedEventTypeException : ProjectorException
{
    const string DefaultMessage = "An unexpected event type was encountered.";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedEventTypeException"/> class with a default error message.
    /// </summary>
    public UnexpectedEventTypeException()
        : base(DefaultMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedEventTypeException"/> class with the specific unexpected event type.
    /// </summary>
    /// <param name="eventType">The runtime type of the unexpected event.</param>
    public UnexpectedEventTypeException(Type? eventType)
        : base($"An unexpected event type was encountered: '{eventType?.FullName ?? eventType?.Name ?? "null"}'.")
    {
        if (eventType is not null)
        {
            Data["EventType"] = eventType.FullName ?? eventType.Name;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedEventTypeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnexpectedEventTypeException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedEventTypeException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnexpectedEventTypeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
