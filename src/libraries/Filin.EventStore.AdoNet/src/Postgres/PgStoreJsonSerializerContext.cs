using System.Text.Json.Serialization;

namespace Filin.EventStore.AdoNet.Postgres;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(PgEventPayload))]
sealed partial class PgStoreJsonSerializerContext : JsonSerializerContext
{
}
