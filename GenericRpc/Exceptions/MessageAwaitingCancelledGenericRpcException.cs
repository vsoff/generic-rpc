using System;

namespace GenericRpc.Exceptions
{
    public class MessageAwaitingCancelledGenericRpcException : Exception
    {
        public MessageAwaitingCancelledGenericRpcException(string message) : base(message)
        {
        }

        public MessageAwaitingCancelledGenericRpcException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}