using System;

namespace GenericRpc.Transport
{
    public sealed class RpcMessage
    {
        public readonly RpcMessageType MessageType;
        public readonly string ServiceName;
        public readonly string MethodName;
        public readonly Guid MessageId;
        public readonly byte[][] RequestData;
        public readonly byte[] ResponseData;

        public RpcMessage(string serviceName, string methodName, Guid messageId, RpcMessageType messageType, byte[][] requestData, byte[] responseData)
        {
            ServiceName = serviceName;
            MethodName = methodName;
            MessageId = messageId;
            MessageType = messageType;
            RequestData = requestData;
            ResponseData = responseData;
        }

        public static readonly RpcMessage KeepAliveMessage = new RpcMessage(null, null, Guid.Empty, RpcMessageType.KeepAlive, null, null);
    }
}