namespace Filin;

public class DcbProjectionStoreException : DcbException
{
    public DcbProjectionStoreException() : base() { }

    public DcbProjectionStoreException(string? message) : base(message) { }

    public DcbProjectionStoreException(string? message, Exception? innerException) : base(message, innerException) { }
}
