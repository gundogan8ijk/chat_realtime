using ProtoBuf;

namespace Chat.Infrastructure.Serializers;

public static class ProtobufSerializer
{
  public static byte[] Serialize<T>(T obj)
  {
    using var stream = new MemoryStream();
    Serializer.Serialize(stream, obj);
    return stream.ToArray();
  }

  public static T Deserialize<T>(byte[] bytes)
  {
    using var stream = new MemoryStream(bytes);
    return Serializer.Deserialize<T>(stream);
  }
}

