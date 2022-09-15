using GenericRpc;
using GenericRpc.ServicesGeneration;

internal class MockDependencyResolver : IListenerDependencyResolver
{
    public object Resolve(Type type, ClientContext context)
    {
        if (type == typeof(ClientExampleService))
            return new ClientExampleService(context);

        if (type == typeof(ServerExampleService))
            return new ServerExampleService(context);

        throw new InvalidOperationException("Unexpected type");
    }
}
