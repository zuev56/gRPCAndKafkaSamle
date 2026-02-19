using System.Text;
using Confluent.Kafka;

namespace Shared;

public static class GuidSerializer
{
    public static readonly ISerializer<Guid> Serializer = new Serializer();

    public static readonly IDeserializer<Guid> Deserializer = new Deserializer();
}

internal sealed class Serializer : ISerializer<Guid>
{
    public byte[] Serialize(Guid data, SerializationContext context)
        => Encoding.UTF8.GetBytes(data.ToString());
}

internal sealed class Deserializer : IDeserializer<Guid>
{
    public Guid Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? Guid.Empty : Guid.Parse(Encoding.UTF8.GetString(data));
}