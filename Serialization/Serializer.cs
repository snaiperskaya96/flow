using System.IO;
using ProtoBuf;

namespace flow.Serialization
{
    public class Serialization
    {
        public static byte[] Serialize<T>(T instance)
        {
            MemoryStream stream = new MemoryStream();
            Serializer.Serialize(stream, instance);
            return stream.ToArray();
        }

        public static T Deserialize<T>(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            return Serializer.Deserialize<T>(stream);
        }
    }
}