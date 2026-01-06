using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Filin.Configuration;
using Filin.Storage;

namespace Filin.Serialization;

[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize(ref Utf8JsonReader, Type, JsonSerializerOptions)")]
[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize(ref Utf8JsonReader, Type, JsonSerializerOptions)")]
sealed class FilinJsonStorageSerializer(IOptions<FilinJsonStorageSerializerOptions> jsonSerializerOptions) : IStorageSerializer
{
    JsonSerializerOptions Options => jsonSerializerOptions.Value.SerializerOptions;

    /// <inheritdoc/>
    public object Deserialize(Type type, Span<byte> input)
    {
        var reader = new Utf8JsonReader(jsonData: input, isFinalBlock: true, state: default);

        return JsonSerializer.Deserialize(ref reader, returnType: type, options: Options) ?? throw new InvalidDataException($"Deserialized result is (null). Expected type: {type.FullName}");
    }

    /// <inheritdoc/>
    public Span<byte> Serialize(object input) => JsonSerializer.SerializeToUtf8Bytes(value: input, inputType: input.GetType(), options: Options);
}
