using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc
{
    public interface IServerCommunicator
    {
        Task StartAsync(string host, int port);
        Task StopAsync();

        TServiceInterface GetProxy<TServiceInterface>(ClientContext context);
        TServiceInterface GetListener<TServiceInterface>(ClientContext context);
    }

    internal sealed class ServerCommunicator : IServerCommunicator
    {
        private readonly IServerTransportLayer _serverTransportLayer;
        private readonly ServicesContainerRoot _servicesContainer;

        internal ServerCommunicator(
            IServerTransportLayer serverTransportLayer,
            ServicesContainerRoot servicesContainer)
        {
            _serverTransportLayer = serverTransportLayer ?? throw new ArgumentException(nameof(serverTransportLayer));
            _servicesContainer = servicesContainer ?? throw new ArgumentException(nameof(servicesContainer));
        }

        public TServiceInterface GetListener<TServiceInterface>(ClientContext context)
            => (TServiceInterface)_servicesContainer.GetServicesContainer(context).GetListenerService(typeof(TServiceInterface));

        public TServiceInterface GetProxy<TServiceInterface>(ClientContext context)
            => (TServiceInterface)_servicesContainer.GetServicesContainer(context).GetProxyService(typeof(TServiceInterface));

        public async Task StartAsync(string host, int port) => await _serverTransportLayer.StartAsync(host, port);

        public async Task StopAsync() => await _serverTransportLayer.StopAsync();
    }
}