using System.Data;

namespace Filin.AdoNet;

interface ICommandInterceptor
{
    void Intercept(IDbCommand command);
}
