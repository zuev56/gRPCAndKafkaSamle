using System.Text;
using Confluent.Kafka;

namespace Shared;

public sealed class GuidUtf8Serializer : ISerializer<Guid>
{
    public byte[] Serialize(Guid data, SerializationContext context) => Encoding.UTF8.GetBytes(data.ToString());
}