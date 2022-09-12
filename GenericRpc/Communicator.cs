using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GenericRpc
{
    public interface IMediator
    {
        object Execute(string serviceName, string methodName, params object[] arguments);
    }

    internal class Mediator : IMediator
    {
        private readonly ConcurrentDictionary<Guid, ResponseAwaiter> _awaiterByMessageId = new ConcurrentDictionary<Guid, ResponseAwaiter>();

        private readonly ICommunicatorSerializer _serializer;
        private readonly ITransportLayer _transportLayer;

        private ServicesContainer _servicesContainer;

        public Mediator(
            ICommunicatorSerializer serializer,
            ITransportLayer transportLayer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _transportLayer = transportLayer ?? throw new ArgumentNullException(nameof(transportLayer));

            transportLayer.OnReceiveMessage += OnReceiveMessage;
        }

        public void SetServicesContainer(ServicesContainer servicesContainer)
        {
            _servicesContainer = servicesContainer ?? throw new ArgumentNullException(nameof(servicesContainer));
        }

        public object Execute(string serviceName, string methodName, params object[] arguments)
        {
            // Get service and method information.
            var servicInterfaceType = _servicesContainer.GetServiceInterfaceType(serviceName);
            var serviceMethod = servicInterfaceType.GetMethod(methodName);
            if (serviceMethod == null)
                throw new GenericRpcException($"Method with name {methodName} not found");

            var argumentTypes = serviceMethod.GetGenericArguments();

            // TODO: In service can be many methods with the same name and arguments count.
            // Serialize arguments.
            if (argumentTypes.Length != arguments.Length)
                throw new GenericRpcException("Arguments count not equals");

            var argumentsBytes = new List<byte[]>(argumentTypes.Length);
            for (int i = 0; i < argumentTypes.Length; i++)
                argumentsBytes[i] = _serializer.Serialize(arguments[i], argumentTypes[i]);

            // Configure awaiter and send message.
            var messageId = Guid.NewGuid();
            var awaiter = new ResponseAwaiter();
            if (!_awaiterByMessageId.TryAdd(messageId, awaiter))
                throw new GenericRpcException($"Message with Id={messageId} already exists");

            var requestMessage = new RpcMessage(serviceName, methodName, Guid.NewGuid(), RpcMessageType.Request, argumentsBytes.ToArray(), null);
            _transportLayer.SendMessageAsync(requestMessage).GetAwaiter().GetResult();
            var response = awaiter.GetResponse();
            _awaiterByMessageId.TryRemove(messageId, out _);

            // Deserialize and return result.
            var resultData = response.ResponseData == null ? null : _serializer.Deserialize(response.ResponseData, serviceMethod.ReturnType);

            return resultData;
        }

        private async void OnReceiveMessage(RpcMessage message, Guid? clientId)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            switch (message.MessageType)
            {
                case RpcMessageType.Request:
                    // Get service and method information.
                    var servicInterfaceType = _servicesContainer.GetServiceInterfaceType(message.ServiceName);
                    var serviceMethod = servicInterfaceType.GetMethod(message.MethodName);
                    if (serviceMethod == null)
                        throw new GenericRpcException($"Method with name {message.MethodName} not found");

                    var argumentTypes = serviceMethod.GetGenericArguments();

                    // TODO: In service can be many methods with the same name and arguments count.
                    // Deserialize arguments.
                    if (argumentTypes.Length != message.RequestData.Length)
                        throw new GenericRpcException("Arguments count not equals");

                    var arguments = new object[argumentTypes.Length];
                    for (int i = 0; i < argumentTypes.Length; i++)
                        arguments[i] = _serializer.Deserialize(message.RequestData[i], argumentTypes[i]);

                    // Invoke method and send result.
                    var service = _servicesContainer.GetListenerService(servicInterfaceType);
                    var result = serviceMethod.Invoke(service, arguments);
                    var resultData = result == null ? null : _serializer.Serialize(result, serviceMethod.ReturnType);
                    await _transportLayer.SendMessageAsync(new RpcMessage(message.ServiceName, message.MethodName, message.MessageId, RpcMessageType.Response, null, resultData), clientId);
                    break;
                case RpcMessageType.Response:
                    if (_awaiterByMessageId.TryGetValue(message.MessageId, out var awaiter))
                    {
                        awaiter.SetResponse(message);
                    }
                    else
                    {
                        // TODO: what we shall do with response without awaiter?
                    }
                    break;
                default: throw new GenericRpcException($"Unknown message type {message.MessageType}");
            }
        }

        /// <remarks>This class is using for awaiting reponse.</remarks>
        private class ResponseAwaiter
        {
            private readonly ManualResetEvent _resetEvent;
            private RpcMessage _message;

            public ResponseAwaiter()
            {
                _resetEvent = new ManualResetEvent(true);
            }

            public void SetResponse(RpcMessage message)
            {
                if (message == null) throw new ArgumentNullException(nameof(message));
                if (_message == null) throw new GenericRpcException($"Message already setted");

                _message = message;
                _resetEvent.Reset();
            }

            public RpcMessage GetResponse()
            {
                _resetEvent.WaitOne();
                return _message;
            }
        }
    }

    internal class ServicesContainer
    {
        private readonly IReadOnlyDictionary<Type, object> _speakerServiceByInterface;
        private readonly IReadOnlyDictionary<Type, object> _listenerServiceByInterface;
        private readonly IReadOnlyDictionary<string, Type> _serviceInterfaceTypeByTypeName;

        public ServicesContainer(
            IReadOnlyDictionary<Type, object> speakerServiceByInterface,
            IReadOnlyDictionary<Type, object> listenerServiceByInterface)
        {
            _speakerServiceByInterface = speakerServiceByInterface ?? throw new ArgumentNullException(nameof(speakerServiceByInterface));
            _listenerServiceByInterface = listenerServiceByInterface ?? throw new ArgumentNullException(nameof(listenerServiceByInterface));
            _serviceInterfaceTypeByTypeName = listenerServiceByInterface.Keys.ToDictionary(x => x.Name, x => x);
        }

        public object GetSpeakerService(Type type)
        {
            if (!type.IsInterface)
                throw new GenericRpcException($"Type `{type.FullName}` isn't an interface");

            if (!_speakerServiceByInterface.TryGetValue(type, out var service))
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

        public Type GetServiceInterfaceType(string interfaceTypeName)
        {
            if (!_serviceInterfaceTypeByTypeName.TryGetValue(interfaceTypeName, out var serviceType))
                throw new GenericRpcException($"Type with name `{interfaceTypeName}` isn't registered");

            return serviceType;
        }
    }

    public sealed class Communicator
    {
        private readonly ServicesContainer _servicesContainer;
        private readonly IMediator _mediator;

        internal Communicator(
            ServicesContainer servicesContainer,
            IMediator mediator)
        {
            _servicesContainer = servicesContainer ?? throw new ArgumentException(nameof(servicesContainer));
            _mediator = mediator ?? throw new ArgumentException(nameof(mediator));
        }

        public TServiceInterface GetSpeakerService<TServiceInterface>()
            => (TServiceInterface)_servicesContainer.GetSpeakerService(typeof(TServiceInterface));
    }
}