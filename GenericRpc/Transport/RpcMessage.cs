using System;

namespace GenericRpc.Transport
{
    public sealed class RpcMessage
    {
        public readonly string ServiceName;
        public readonly string MethodName;
        public readonly Guid MessageId;
        public readonly RpcMessageType MessageType;
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
    }
}