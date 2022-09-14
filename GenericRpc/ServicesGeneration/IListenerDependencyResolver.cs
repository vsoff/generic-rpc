using System;

namespace GenericRpc.ServicesGeneration
{
    public interface IListenerDependencyResolver
    {
        object Resolve(Type type, ClientContext context);
    }
}