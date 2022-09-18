using GenericRpc.Serialization;
using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Mediators
{
    internal sealed class ServerMediator : BaseMediator
    {
        private readonly AwaitingMessagesContainerRoot _awaitersContainer;
        private readonly IServerTransportLayer _serverTransportLayer;

        private bool _disposed = false;

        public ServerMediator(
            IServerTransportLayer serverTransportLayer,
            ICommunicatorSerializer serializer)
            : base(serializer)
        {
            _serverTransportLayer = serverTransportLayer ?? throw new ArgumentNullException(nameof(serverTransportLayer));
            _awaitersContainer = new AwaitingMessagesContainerRoot();

            serverTransportLayer.OnMessageReceived += OnReceiveMessage;
            serverTransportLayer.OnClientConnected += OnClientConnected;
            serverTransportLayer.OnClientDisconnected += OnClientDisconnected;
            serverTransportLayer.OnServerShutdown += OnServerShutdown;
        }

        protected override object GetListenerService(ClientContext clientContext, Type serviceInterfaceType)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (clientContext == null) throw new ArgumentNullException(nameof(clientContext));
            return ServicesContainer.GetServicesContainer(clientContext).GetListenerService(serviceInterfaceType);
        }

        protected override async Task SendMessageAsync(ClientContext clientContext, RpcMessage message)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (clientContext == null) throw new ArgumentNullException(nameof(clientContext));
            await _serverTransportLayer.SendMessageAsync(message, clientContext);
        }

        protected override void CreateResponseAwaiter(ClientContext clientContext, Guid messageId)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            _awaitersContainer.GetServicesContainer(clientContext).CreateResponseAwaiter(messageId);
        }

        protected override RpcMessage AwaitResponse(ClientContext clientContext, Guid messageId)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            return _awaitersContainer.GetServicesContainer(clientContext).AwaitResponse(messageId);
        }

        protected override void SetResponse(ClientContext clientContext, RpcMessage responseMessage)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            _awaitersContainer.GetServicesContainer(clientContext).SetResponse(responseMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _serverTransportLayer.OnMessageReceived -= OnReceiveMessage;
                _serverTransportLayer.OnClientConnected -= OnClientConnected;
                _serverTransportLayer.OnClientDisconnected -= OnClientDisconnected;
                _serverTransportLayer.OnServerShutdown -= OnServerShutdown;

                _awaitersContainer.Dispose();
            }

            base.Dispose(disposing);
            _disposed = true;
        }

        private void OnServerShutdown()
        {
            _awaitersContainer.Clear();
        }

        private void OnClientConnected(ClientContext clientContext)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            ServicesContainer.RegisterServicesForClientContext(clientContext);
            _awaitersContainer.RegisterServicesForClientContext(clientContext);
        }

        private void OnClientDisconnected(ClientContext clientContext)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            ServicesContainer.UnregisterServicesForClientContext(clientContext);
            _awaitersContainer.UnregisterServicesForClientContext(clientContext);
        }
    }
}