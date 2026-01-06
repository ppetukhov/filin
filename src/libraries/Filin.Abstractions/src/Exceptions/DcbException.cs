namespace Filin;

public abstract class DcbException : Exception
{
    protected DcbException() : base() { }

    protected DcbException(string? message) : base(message) { }

    protected DcbException(string? message, Exception? innerException) : base(message, innerException) { }
}
