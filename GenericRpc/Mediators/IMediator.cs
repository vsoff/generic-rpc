namespace GenericRpc.Mediators
{
    public interface IMediator
    {
        object Execute(ClientContext clientContext, string serviceName, string methodName, params object[] arguments);
    }
}