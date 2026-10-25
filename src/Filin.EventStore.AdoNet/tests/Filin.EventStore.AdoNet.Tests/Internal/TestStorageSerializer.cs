using System.Text;

using Filin.Storage;

namespace Filin.EventStore.AdoNet.Tests.Internal;

public class TestStorageSerializer : IStorageSerializer
{
    public byte[] ResponseData { get; set; } = Encoding.UTF8.GetBytes("{}");
    public int SerializeCallCount { get; private set; }

    public Span<byte> Serialize(object input)
    {
        SerializeCallCount++;
        return ResponseData;
    }

    public object Deserialize(Type returnType, Span<byte> input)
    {
        return new object();
    }
}
