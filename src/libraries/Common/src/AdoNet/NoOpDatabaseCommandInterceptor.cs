using System.Data;

namespace Filin.AdoNet;

sealed class NoOpCommandInterceptor : ICommandInterceptor
{
    public static readonly ICommandInterceptor Instance = new NoOpCommandInterceptor();

    NoOpCommandInterceptor()
    {

    }

    public void Intercept(IDbCommand command)
    {
        //NOP
    }
}
