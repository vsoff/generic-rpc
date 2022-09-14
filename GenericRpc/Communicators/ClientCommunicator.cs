using GenericRpc.ServicesGeneration;
using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Communicators
{
    public interface IClientCommunicator
    {
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();

        TServiceInterface GetProxy<TServiceInterface>();
        TServiceInterface GetListener<TServiceInterface>();
    }

    internal sealed class ClientCommunicator : IClientCommunicator
    {
        private readonly IClientTransportLayer _clientTransportLayer;
        private readonly ServicesContainerRoot _servicesContainer;

        internal ClientCommunicator(
            IClientTransportLayer clientTransportLayer,
            ServicesContainerRoot servicesContainer)
        {
            _clientTransportLayer = clientTransportLayer ?? throw new ArgumentException(nameof(clientTransportLayer));
            _servicesContainer = servicesContainer ?? throw new ArgumentException(nameof(servicesContainer));
        }

        public async Task ConnectAsync(string host, int port) => await _clientTransportLayer.ConnectAsync(host, port);

        public async Task DisconnectAsync() => await _clientTransportLayer.DisconnectAsync();

        public TServiceInterface GetListener<TServiceInterface>()
            => (TServiceInterface)_servicesContainer.ClientContainer.GetListenerService(typeof(TServiceInterface));

        public TServiceInterface GetProxy<TServiceInterface>()
            => (TServiceInterface)_servicesContainer.ClientContainer.GetProxyService(typeof(TServiceInterface));
    }
}