using GenericRpc.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GenericRpc.ServicesGeneration
{
    internal class ServicesContainerRoot
    {
        private readonly ConcurrentDictionary<ClientContext, Container> _clientServicesByContext;

        private readonly IReadOnlyDictionary<string, ServiceTypesInfo> _serviceTypeInfoByName;

        public readonly IContainer ClientContainer;
        private readonly IListenerDependencyResolver _dependencyResolver;
        private readonly IMediator _mediator;

        public ServicesContainerRoot(
            IReadOnlyCollection<ServiceTypesInfo> serviceTypesInfos,
            IListenerDependencyResolver dependencyResolver,
            IMediator mediator)
        {
            _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            if (serviceTypesInfos == null) throw new ArgumentNullException(nameof(serviceTypesInfos));

            _clientServicesByContext = new ConcurrentDictionary<ClientContext, Container>();
            _serviceTypeInfoByName = serviceTypesInfos.ToDictionary(x => x.ServiceName, x => x);

            ClientContainer = BuildContainer();
        }

        public IContainer GetServicesContainer(ClientContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!_clientServicesByContext.TryGetValue(context, out var container))
                throw new GenericRpcException($"Services container for context with id = {context.Id} isn't registered");

            return container;
        }

        public void RegisterServicesForClientContext(ClientContext context)
        {
            if (!_clientServicesByContext.TryAdd(context, BuildContainer(context)))
                throw new GenericRpcException("Error while register services for client");
        }

        public void UnregisterServicesForClientContext(ClientContext context)
        {
            if (!_clientServicesByContext.TryRemove(context, out _))
                throw new GenericRpcException("Error while unregister services for client");
        }

        public Type GetServiceInterfaceType(string interfaceTypeName)
        {
            if (!_serviceTypeInfoByName.TryGetValue(interfaceTypeName, out var serviceTypeInfo))
                throw new GenericRpcException($"Type with name `{interfaceTypeName}` isn't registered");

            return serviceTypeInfo.InterfaceType;
        }

        private Container BuildContainer(ClientContext context = null)
        {
            var proxyServiceByInterface = _serviceTypeInfoByName.Values.Where(x => x.IsProxy)
                .ToDictionary(x => x.InterfaceType, x => ProxyGenerator.ActivateProxyInstance(x.RealizationType, _mediator, context));

            var listenerServiceByInterface = _serviceTypeInfoByName.Values.Where(x => !x.IsProxy)
                .ToDictionary(x => x.InterfaceType, x => _dependencyResolver.Resolve(x.RealizationType, context));

            return new Container(proxyServiceByInterface, listenerServiceByInterface);
        }

        private class Container : IContainer
        {
            private readonly IReadOnlyDictionary<Type, object> _proxyServiceByInterface;
            private readonly IReadOnlyDictionary<Type, object> _listenerServiceByInterface;

            public Container(
                IReadOnlyDictionary<Type, object> proxyServiceByInterface,
                IReadOnlyDictionary<Type, object> listenerServiceByInterface)
            {
                _proxyServiceByInterface = proxyServiceByInterface ?? throw new ArgumentNullException(nameof(proxyServiceByInterface));
                _listenerServiceByInterface = listenerServiceByInterface ?? throw new ArgumentNullException(nameof(listenerServiceByInterface));
            }

            public object GetProxyService(Type type)
            {
                if (!type.IsInterface)
                    throw new GenericRpcException($"Type `{type.FullName}` isn't an interface");

                if (!_proxyServiceByInterface.TryGetValue(type, out var service))
                    throw new GenericRpcException($"Speaker service with interface `{type.FullName}` isn't found");

                return service;
            }

            public object GetListenerService(Type type)
            {
                if (!type.IsInterface)
                    throw new GenericRpcException($"Type `{type.FullName}` isn't an interface");

                if (!_listenerServiceByInterface.TryGetValue(type, out var service))
                    throw new GenericRpcException($"Listener service with interface `{type.FullName}` isn't found");

                return service;
            }
        }

        public interface IContainer
        {
            object GetProxyService(Type type);
            object GetListenerService(Type type);
        }
    }
}