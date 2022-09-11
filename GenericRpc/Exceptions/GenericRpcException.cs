using System;

namespace GenericRpc.Exceptions
{
    public class GenericRpcException : Exception
    {
        public GenericRpcException(string message) : base(message)
        {
        }

        public GenericRpcException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}