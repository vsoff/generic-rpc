using GenericRpc.Exceptions;
using System;

namespace GenericRpc.SocketTransport.Common
{
    public class GenericRpcSocketTransportException : GenericRpcException
    {
        public GenericRpcSocketTransportException(string message) : base(message)
        {
        }

        public GenericRpcSocketTransportException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class GenericRpcSocketOfflineException : GenericRpcSocketTransportException
    {
        public GenericRpcSocketOfflineException() : base("Socket is not alive")
        {
        }
    }
}