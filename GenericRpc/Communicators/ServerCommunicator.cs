using GenericRpc.Mediators;
using GenericRpc.ServicesGeneration;
using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Communicators
{
    public interface IServerCommunicator : IDisposable
    {
        event ClientConnected OnClientConnected;
        event ClientDisconnected OnClientDisconnected;
        event ServerShutdown OnServerShutdown;

        Task StartAsync(string host, int port);
        Task StopAsync();

        Task DisconnectClientAsync(ClientContext clientContext);
        TServiceInterface GetProxy<TServiceInterface>(ClientContext context);
    }

    internal sealed class ServerCommunicator : IServerCommunicator
    {
        private bool _disposed;

        private readonly IServerTransportLayer _serverTransportLayer;
        private readonly ServicesContainerRoot _servicesContainer;
        private readonly IMediator _mediator;

        public event ClientConnected OnClientConnected;
        public event ClientDisconnected OnClientDisconnected;
        public event ServerShutdown OnServerShutdown;

        internal ServerCommunicator(
            IServerTransportLayer serverTransportLayer,
            ServicesContainerRoot servicesContainer,
            IMediator mediator)
        {
            _serverTransportLayer = serverTransportLayer ?? throw new ArgumentException(nameof(serverTransportLayer));
            _servicesContainer = servicesContainer ?? throw new ArgumentException(nameof(servicesContainer));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            serverTransportLayer.OnServerShutdown += ServerTransportLayer_OnServerShutdown;
            serverTransportLayer.OnClientConnected += ServerTransportLayer_OnClientConnected;
            serverTransportLayer.OnClientDisconnected += ServerTransportLayer_OnClientDisconnected;
        }

        public TServiceInterface GetProxy<TServiceInterface>(ClientContext context)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            return (TServiceInterface)_servicesContainer.GetServicesContainer(context).GetProxyService(typeof(TServiceInterface));
        }

        public async Task StartAsync(string host, int port)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            await _serverTransportLayer.StartAsync(host, port);
        }

        public async Task StopAsync()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            await _serverTransportLayer.StopAsync();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _serverTransportLayer.OnServerShutdown -= ServerTransportLayer_OnServerShutdown;
            _serverTransportLayer.OnClientConnected -= ServerTransportLayer_OnClientConnected;
            _serverTransportLayer.OnClientDisconnected -= ServerTransportLayer_OnClientDisconnected;
            _serverTransportLayer.Dispose();
            _mediator.Dispose();

            _disposed = true;
        }

        public async Task DisconnectClientAsync(ClientContext clientContext) => await _serverTransportLayer.DisconnectClientAsync(clientContext);

        private void ServerTransportLayer_OnServerShutdown() => Task.Run(() => OnServerShutdown?.Invoke());
        private void ServerTransportLayer_OnClientConnected(ClientContext context) => Task.Run(() => OnClientConnected?.Invoke(context));
        private void ServerTransportLayer_OnClientDisconnected(ClientContext context) => Task.Run(() => OnClientDisconnected?.Invoke(context));
    }
}