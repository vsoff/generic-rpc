using GenericRpc.Exceptions;
using GenericRpc.Mediators;
using GenericRpc.Serialization;
using GenericRpc.ServicesGeneration;
using GenericRpc.Transport;
using System;
using System.Collections.Generic;

namespace GenericRpc.Communicators
{
    public sealed class CommunicatorBuilder
    {
        private readonly Dictionary<Type, Type> _listenServiceTypeByInterface = new Dictionary<Type, Type>();
        private readonly HashSet<Type> _proxyServiceTypes = new HashSet<Type>();

        private ICommunicatorSerializer _serializer;
        private IServerTransportLayer _serverTransportLayer;
        private IClientTransportLayer _clientTransportLayer;
        private IListenerDependencyResolver _dependencyResolver;

        public CommunicatorBuilder SetSerializer(ICommunicatorSerializer serializer)
        {
            if (_serializer != null) throw new GenericRpcException("Serializer already setted");
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            return this;
        }

        public CommunicatorBuilder SetDependencyResolver(IListenerDependencyResolver dependencyResolver)
        {
            if (_dependencyResolver != null) throw new GenericRpcException("Dependency resolver already setted");
            _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
            return this;
        }

        public CommunicatorBuilder SetServerTransportLayer(IServerTransportLayer serverTransportLayer)
        {
            if (_serverTransportLayer != null) throw new GenericRpcException("Server transport layer already setted");
            if (_clientTransportLayer != null) throw new GenericRpcException("Client transport layer already setted");
            _serverTransportLayer = serverTransportLayer ?? throw new ArgumentNullException(nameof(serverTransportLayer));
            return this;
        }

        public CommunicatorBuilder SetClientTransportLayer(IClientTransportLayer clientTransportLayer)
        {
            if (_serverTransportLayer != null) throw new GenericRpcException("Server transport layer already setted");
            if (_clientTransportLayer != null) throw new GenericRpcException("Client transport layer already setted");
            _clientTransportLayer = clientTransportLayer ?? throw new ArgumentNullException(nameof(clientTransportLayer));
            return this;
        }

        public CommunicatorBuilder RegisterProxyService<TServiceInterface>()
        {
            if (!typeof(TServiceInterface).IsInterface)
                throw new GenericRpcException($"Type `{nameof(TServiceInterface)}` isn't an interface");

            if (_proxyServiceTypes.Contains(typeof(TServiceInterface)))
                throw new GenericRpcException($"Proxy for interface `{nameof(TServiceInterface)}` already registered");

            _proxyServiceTypes.Add(typeof(TServiceInterface));
            return this;
        }

        public CommunicatorBuilder RegisterListenerService<TServiceInterface, TServiceRealization>()
            where TServiceRealization : TServiceInterface
        {
            if (!typeof(TServiceInterface).IsInterface)
                throw new GenericRpcException($"Type `{nameof(TServiceInterface)}` isn't an interface");

            if (_listenServiceTypeByInterface.ContainsKey(typeof(TServiceInterface)))
                throw new GenericRpcException($"Realization for interface `{nameof(TServiceInterface)}` already registered");

            _listenServiceTypeByInterface.Add(typeof(TServiceInterface), typeof(TServiceRealization));
            return this;
        }

        public IServerCommunicator BuildServer()
        {
            if (_serializer == null) throw new GenericRpcException("Serializer not setted");
            if (_serverTransportLayer == null) throw new GenericRpcException("Server transport layer not setted");

            var mediator = new ServerMediator(_serverTransportLayer, _serializer);
            var serviceContainer = BuildServiceContainerRoot(mediator);
            mediator.SetServicesContainer(serviceContainer);

            return new ServerCommunicator(_serverTransportLayer, serviceContainer);
        }

        public IClientCommunicator BuildClient()
        {
            if (_serializer == null) throw new GenericRpcException("Serializer not setted");
            if (_clientTransportLayer == null) throw new GenericRpcException("Client transport layer not setted");

            var mediator = new ClientMediator(_clientTransportLayer, _serializer);
            var serviceContainer = BuildServiceContainerRoot(mediator);
            mediator.SetServicesContainer(serviceContainer);

            return new ClientCommunicator(_clientTransportLayer, serviceContainer);
        }

        private ServicesContainerRoot BuildServiceContainerRoot(IMediator mediator)
        {
            var servicesTypeInfos = new List<ServiceTypesInfo>();

            foreach (var typesPair in _listenServiceTypeByInterface)
            {
                servicesTypeInfos.Add(new ServiceTypesInfo(typesPair.Key, typesPair.Value, false));
            }

            foreach (var proxyInterfaceType in _proxyServiceTypes)
            {
                var proxyRealizationType = ProxyGenerator.GenerateProxyType(proxyInterfaceType);
                servicesTypeInfos.Add(new ServiceTypesInfo(proxyInterfaceType, proxyRealizationType, true));
            }

            return new ServicesContainerRoot(servicesTypeInfos, _dependencyResolver, mediator);
        }
    }
}
