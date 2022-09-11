using GenericRpc.Transport;
using System;
using System.IO;

namespace GenericRpc.SocketTransport
{
    public static class RpcMessageSerializer
    {
        public static RpcMessage Deserialize(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                var source = Read(reader);
                if (stream.Position != stream.Length)
                    throw new GenericRpcSocketTransportException($"Data not fully readed from stream. Position: {stream.Position}, Length: {stream.Length}");

                return source;
            }
        }
        public static byte[] Serialize(RpcMessage message)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Write(writer, message);
                var data = stream.ToArray();

                return data;
            }
        }

        public static RpcMessage Read(BinaryReader reader) => throw new NotImplementedException();
        public static void Write(BinaryWriter writer, RpcMessage source) => throw new NotImplementedException();
    }
}