using System;

namespace GenericRpc.Exceptions
{
    public class GenericRpcSerializationException : GenericRpcException
    {
        public GenericRpcSerializationException(string message) : base(message)
        {
        }

        public GenericRpcSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}