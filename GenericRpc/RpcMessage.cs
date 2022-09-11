using System;

namespace GenericRpc
{
    public class RpcMessage
    {
        public readonly string ServiceName;
        public readonly string MethodName;
        public readonly Guid MessageId;
        public readonly RpcMessageType MessageType;
        public readonly byte[] Data;

        public RpcMessage(string serviceName, string methodName, Guid messageId, RpcMessageType messageType, byte[] data)
        {
            ServiceName = serviceName;
            MethodName = methodName;
            MessageId = messageId;
            MessageType = messageType;
            Data = data;
        }
    }
}