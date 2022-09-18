using GenericRpc.Mediators;
using GenericRpc.ServicesGeneration;
using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Communicators
{
    public interface IServerCommunicator : IDisposable
    {
        Task StartAsync(string host, int port);
        Task StopAsync();

        TServiceInterface GetProxy<TServiceInterface>(ClientContext context);
    }

    internal sealed class ServerCommunicator : IServerCommunicator
    {
        private bool _disposed;

        private readonly IServerTransportLayer _serverTransportLayer;
        private readonly ServicesContainerRoot _servicesContainer;
        private readonly IMediator _mediator;

        internal ServerCommunicator(
            IServerTransportLayer serverTransportLayer,
            ServicesContainerRoot servicesContainer,
            IMediator mediator)
        {
            _serverTransportLayer = serverTransportLayer ?? throw new ArgumentException(nameof(serverTransportLayer));
            _servicesContainer = servicesContainer ?? throw new ArgumentException(nameof(servicesContainer));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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

            _serverTransportLayer.Dispose();
            _mediator.Dispose();

            _disposed = true;
        }
    }
}