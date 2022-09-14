using System;

namespace GenericRpc.SocketTransport.Common
{
    public sealed class CommunicationErrorInfo
    {
        public readonly string Message;
        public readonly Exception Exception;

        internal CommunicationErrorInfo(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}