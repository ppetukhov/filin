namespace Filin;

public abstract class DcbProjectorException : DcbException
{
    protected DcbProjectorException() : base() { }

    protected DcbProjectorException(string? message) : base(message) { }

    protected DcbProjectorException(string? message, Exception? innerException) : base(message, innerException) { }
}
