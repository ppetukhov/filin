using System.Text.Json.Serialization;

namespace Filin.EventStore.Postgres;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(PgEventPayload))]
sealed partial class StoreJsonSerializerContext : JsonSerializerContext
{

}
