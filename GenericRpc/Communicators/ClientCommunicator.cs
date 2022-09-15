using GenericRpc.Mediators;
using GenericRpc.ServicesGeneration;
using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Communicators
{
    public interface IClientCommunicator : IDisposable
    {
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();

        TServiceInterface GetProxy<TServiceInterface>();
        TServiceInterface GetListener<TServiceInterface>();
    }

    internal sealed class ClientCommunicator : IClientCommunicator
    {
        private bool _disposed;

        private readonly IClientTransportLayer _clientTransportLayer;
        private readonly ServicesContainerRoot _servicesContainer;
        private readonly IMediator _mediator;

        internal ClientCommunicator(
            IClientTransportLayer clientTransportLayer,
            ServicesContainerRoot servicesContainer,
            IMediator mediator)
        {
            _clientTransportLayer = clientTransportLayer ?? throw new ArgumentException(nameof(clientTransportLayer));
            _servicesContainer = servicesContainer ?? throw new ArgumentException(nameof(servicesContainer));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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

        public TServiceInterface GetListener<TServiceInterface>()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            return (TServiceInterface)_servicesContainer.ClientContainer.GetListenerService(typeof(TServiceInterface));
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

            _clientTransportLayer.Dispose();
            _mediator.Dispose();

            _disposed = true;
        }
    }
}