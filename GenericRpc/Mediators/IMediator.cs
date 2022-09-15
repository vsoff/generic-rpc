using System;

namespace GenericRpc.Mediators
{
    public interface IMediator : IDisposable
    {
        object Execute(ClientContext clientContext, string serviceName, string methodName, params object[] arguments);
    }
}