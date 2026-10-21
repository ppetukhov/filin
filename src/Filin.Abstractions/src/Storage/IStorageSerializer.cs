namespace Filin.Storage;

/// <summary>
/// Common interface for grain state serializers.
/// </summary>
public interface IStorageSerializer
{
    /// <summary>
    /// Serializes the object input.
    /// </summary>
    /// <param name="input">The object to serialize.</param>
    /// <returns>The serialized input.</returns>
    Span<byte> Serialize(object input);

    /// <summary>
    /// Deserializes the provided data.
    /// </summary>
    /// <param name="input">The data to deserialize.</param>
    /// <param name="returnType">The output type.</param>
    /// <returns>The deserialized object.</returns>
    object Deserialize(Type returnType, Span<byte> input);
}
