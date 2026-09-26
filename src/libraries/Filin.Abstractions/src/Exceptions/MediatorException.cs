namespace Filin;

public class MediatorException : DcbException
{
    public MediatorException() : base() { }

    public MediatorException(string? message) : base(message) { }

    public MediatorException(string? message, Exception? innerException) : base(message, innerException) { }
}
