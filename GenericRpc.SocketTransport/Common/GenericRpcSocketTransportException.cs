using System;

namespace GenericRpc.SocketTransport.Common
{
    public class GenericRpcSocketTransportException : Exception
    {
        public GenericRpcSocketTransportException(string message) : base(message)
        {
        }

        public GenericRpcSocketTransportException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}