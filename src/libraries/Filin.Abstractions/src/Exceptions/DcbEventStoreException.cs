namespace Filin;

public class DcbEventStoreException : DcbException
{
    public DcbEventStoreException() : base() { }

    public DcbEventStoreException(string? message) : base(message) { }

    public DcbEventStoreException(string? message, Exception? innerException) : base(message, innerException) { }
}
