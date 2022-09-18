using GenericRpc.Serialization;
using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Mediators
{
    internal sealed class ClientMediator : BaseMediator
    {
        private readonly AwaitingMessagesContainerRoot _awaitersContainer;
        private readonly IClientTransportLayer _clientTransportLayer;

        private bool _disposed = false;

        public ClientMediator(
            IClientTransportLayer clientTransportLayer,
            ICommunicatorSerializer serializer)
            : base(serializer)
        {
            _clientTransportLayer = clientTransportLayer ?? throw new ArgumentNullException(nameof(clientTransportLayer));
            _awaitersContainer = new AwaitingMessagesContainerRoot();

            clientTransportLayer.OnMessageReceived += MessageReceived;
            clientTransportLayer.OnDisconnected += OnDisconnected;
        }

        protected override object GetListenerService(ClientContext clientContext, Type serviceInterfaceType)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            return ServicesContainer.ClientContainer.GetListenerService(serviceInterfaceType);
        }

        protected override async Task SendMessageAsync(ClientContext clientContext, RpcMessage message)
        {

            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            await _clientTransportLayer.SendMessageAsync(message);
        }

        protected override void CreateResponseAwaiter(ClientContext clientContext, Guid messageId)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            _awaitersContainer.ClientContainer.CreateResponseAwaiter(messageId);
        }

        protected override RpcMessage AwaitResponse(ClientContext clientContext, Guid messageId)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            return _awaitersContainer.ClientContainer.AwaitResponse(messageId);
        }

        protected override void SetResponse(ClientContext clientContext, RpcMessage responseMessage)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            _awaitersContainer.ClientContainer.SetResponse(responseMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _clientTransportLayer.OnMessageReceived -= MessageReceived;
                _awaitersContainer.Dispose();
            }

            base.Dispose(disposing);
            _disposed = true;
        }

        private void OnDisconnected()
        {
            _awaitersContainer.Clear();
        }

        private async Task MessageReceived(RpcMessage message) => await OnReceiveMessage(message, null);
    }
}