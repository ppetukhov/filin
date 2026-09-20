using Filin;

namespace Edu.Exceptions;

public sealed class InvalidStateException : DcbProjectorException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedEventTypeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidStateException(string message)
        : base(message)
    {
    }
}
