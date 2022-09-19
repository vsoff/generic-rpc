using GenericRpc.Exceptions;
using GenericRpc.Transport;
using System;
using System.IO;

namespace GenericRpc.SocketTransport.Common
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
                    throw new GenericRpcSerializationException($"Data not fully readed from stream. Position: {stream.Position}, Length: {stream.Length}");

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

        public static RpcMessage Read(BinaryReader reader)
        {
            var messageType = (RpcMessageType)reader.ReadByte();
            if (messageType == RpcMessageType.KeepAlive)
                return RpcMessage.KeepAliveMessage;

            byte[][] requestData = null;
            byte[] resoponseData = null;

            const int guidBytesCount = 16;
            var serviceName = reader.ReadString();
            var methodName = reader.ReadString();
            var messageIdBytes = reader.ReadBytes(guidBytesCount);
            var messageId = new Guid(messageIdBytes);
            var exceptionInfo = ReadExceptionInfo(reader);

            switch (messageType)
            {
                case RpcMessageType.Request:
                    {
                        var length = reader.ReadInt32();
                        requestData = new byte[length][];
                        for (int i = 0; i < length; i++)
                        {
                            var argumentLength = reader.ReadInt32();
                            var argumentBytes = reader.ReadBytes(argumentLength);
                            requestData[i] = argumentBytes;
                        }
                        break;
                    }
                case RpcMessageType.Response:
                    {
                        var isNull = reader.ReadBoolean();
                        if (!isNull)
                        {
                            var length = reader.ReadInt32();
                            resoponseData = reader.ReadBytes(length);
                        }
                        break;
                    }
                default: throw new GenericRpcSerializationException($"Unexpected type of {nameof(RpcMessageType)}");
            }

            return new RpcMessage(serviceName, methodName, messageId, messageType, requestData, resoponseData, exceptionInfo);
        }

        public static void Write(BinaryWriter writer, RpcMessage source)
        {
            writer.Write((byte)source.MessageType);
            if (source.MessageType == RpcMessageType.KeepAlive)
                return;

            writer.Write(source.ServiceName);
            writer.Write(source.MethodName);
            writer.Write(source.MessageId.ToByteArray());
            WriteExceptionInfo(writer, source.RemoteException);
            switch (source.MessageType)
            {
                case RpcMessageType.Request:
                    {
                        writer.Write(source.RequestData.Length);
                        for (int i = 0; i < source.RequestData.Length; i++)
                        {
                            var argumentData = source.RequestData[i];
                            writer.Write(argumentData.Length);
                            writer.Write(argumentData);
                        }
                        break;
                    }
                case RpcMessageType.Response:
                    {
                        bool isNull = source.ResponseData == null;
                        writer.Write(isNull);
                        if (!isNull)
                        {
                            writer.Write(source.ResponseData.Length);
                            writer.Write(source.ResponseData);
                        }
                        break;
                    }
                default: throw new GenericRpcSerializationException($"Unexpected type of {nameof(RpcMessageType)}");
            }
        }

        private static RemoteExceptionInfo ReadExceptionInfo(BinaryReader reader)
        {
            var isNull = reader.ReadBoolean();
            if (isNull)
                return null;

            var errorText = reader.ReadString();
            return new RemoteExceptionInfo(errorText);
        }

        private static void WriteExceptionInfo(BinaryWriter writer, RemoteExceptionInfo source)
        {
            var isNull = source == null;
            writer.Write(isNull);
            if (isNull)
                return;

            writer.Write(source.ErrorText);
        }
    }
}