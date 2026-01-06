namespace Filin;

public sealed class DcbConflictException : DcbException
{
    public DcbConflictException() : base() { }

    public DcbConflictException(string? message) : base(message) { }

    public DcbConflictException(string? message, Exception? innerException) : base(message, innerException) { }
}
