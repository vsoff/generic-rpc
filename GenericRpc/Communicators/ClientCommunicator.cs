using GenericRpc.Mediators;
using GenericRpc.ServicesGeneration;
using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Communicators
{
    public interface IClientCommunicator : IDisposable
    {
        event OnDisconnected OnDisconnected;

        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();

        TServiceInterface GetProxy<TServiceInterface>();
    }

    internal sealed class ClientCommunicator : IClientCommunicator
    {
        private bool _disposed;

        private readonly IClientTransportLayer _clientTransportLayer;
        private readonly ServicesContainerRoot _servicesContainer;
        private readonly IMediator _mediator;

        public event OnDisconnected OnDisconnected;

        internal ClientCommunicator(
            IClientTransportLayer clientTransportLayer,
            ServicesContainerRoot servicesContainer,
            IMediator mediator)
        {
            _clientTransportLayer = clientTransportLayer ?? throw new ArgumentException(nameof(clientTransportLayer));
            _servicesContainer = servicesContainer ?? throw new ArgumentException(nameof(servicesContainer));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            clientTransportLayer.OnDisconnected += ClientTransportLayer_OnDisconnected;
        }

        public async Task ConnectAsync(string host, int port)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            await _clientTransportLayer.ConnectAsync(host, port);
        }

        public async Task DisconnectAsync()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            await _clientTransportLayer.DisconnectAsync();
        }

        public TServiceInterface GetProxy<TServiceInterface>()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            return (TServiceInterface)_servicesContainer.ClientContainer.GetProxyService(typeof(TServiceInterface));
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;

            _clientTransportLayer.OnDisconnected -= ClientTransportLayer_OnDisconnected;
            _clientTransportLayer.Dispose();
            _mediator.Dispose();

            _disposed = true;
        }

        private void ClientTransportLayer_OnDisconnected() => Task.Run(() => OnDisconnected?.Invoke());
    }
}