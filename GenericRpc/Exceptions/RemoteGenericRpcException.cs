using GenericRpc.Transport;
using System;

namespace GenericRpc.Exceptions
{
    public sealed class RemoteGenericRpcException : Exception
    {
        public RemoteGenericRpcException(RemoteExceptionInfo info) : base(info.ErrorText)
        {
        }
    }
}