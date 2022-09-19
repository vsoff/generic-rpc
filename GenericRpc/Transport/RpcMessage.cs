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
        public readonly RemoteExceptionInfo RemoteException;

        public RpcMessage(
            string serviceName, 
            string methodName, 
            Guid messageId, 
            RpcMessageType messageType, 
            byte[][] requestData, 
            byte[] responseData, 
            RemoteExceptionInfo remoteException)
        {
            ServiceName = serviceName;
            MethodName = methodName;
            MessageId = messageId;
            MessageType = messageType;
            RequestData = requestData;
            ResponseData = responseData;
            RemoteException = remoteException;
        }

        public static readonly RpcMessage KeepAliveMessage = new RpcMessage(null, null, Guid.Empty, RpcMessageType.KeepAlive, null, null, null);
    }

    public sealed class RemoteExceptionInfo
    {
        public readonly string ErrorText;

        public RemoteExceptionInfo(string errorText)
        {
            ErrorText = errorText;
        }
    }
}