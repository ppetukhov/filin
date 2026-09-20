using Npgsql;

using Polly;

namespace Filin.EventStore.Postgres.Data;

static class PostgresTransientErrors
{
    /// <summary>
    /// A <see cref="PredicateBuilder"/> that matches any <see cref="NpgsqlException"/> whose
    /// <see cref="PostgresException.SqlState"/> is a known transient PostgreSQL error code.
    /// </summary>
    public static PredicateBuilder<object> IsTransient =>
        new PredicateBuilder<object>().Handle<NpgsqlException>(ex => ex.IsTransient);
}
