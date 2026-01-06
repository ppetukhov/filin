using Filin.Storage;

namespace Filin.Configuration;

/// <summary>
/// Interface to be implemented by the storage provider options.
/// </summary>
public interface IStorageProviderSerializerOptions
{
    /// <summary>
    /// Gets or sets the serializer to use for this storage provider.
    /// </summary>
    IStorageSerializer StorageSerializer { get; set; }
}
