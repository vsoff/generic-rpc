using System;

namespace GenericRpc
{
    public interface IListenerDependencyResolver
    {
        object Resolve(Type type, ClientContext context);
    }
}