namespace Edu;

static class ExceptionExtensions
{
    extension<TException>(TException exception) where TException : Exception
    {
        internal TException EnrichWithProjector(Type projectorType)
        {
            exception.Data["Projector"] = projectorType.FullName ?? projectorType.Name;

            return exception;
        }
    }
}
