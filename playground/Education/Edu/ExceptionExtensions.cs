namespace Edu;

static class ExceptionExtensions
{
    internal static TException EnrichWithProjector<TException>(this TException exception, Type projectorType)
        where TException : Exception
    {
        exception.Data["Projector"] = projectorType.FullName ?? projectorType.Name;

        return exception;
    }
}
