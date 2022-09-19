namespace GenericRpc
{
    public abstract class ListenerService
    {
        private ClientContext _clientContext;
        protected ClientContext ClientContext => _clientContext;

        internal void SetClientContext(ClientContext context) => _clientContext = context;
    }
}
