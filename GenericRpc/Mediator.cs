using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.ServicesGeneration;
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
        object Execute(ClientContext clientContext, string serviceName, string methodName, params object[] arguments);
    }

    internal class Mediator : IMediator
    {
        private readonly ConcurrentDictionary<Guid, ResponseAwaiter> _awaiterByMessageId = new ConcurrentDictionary<Guid, ResponseAwaiter>();
        private readonly bool _isServer;

        private readonly ICommunicatorSerializer _serializer;
        private readonly IClientTransportLayer _clientTransportLayer;
        private readonly IServerTransportLayer _serverTransportLayer;

        private ServicesContainerRoot _servicesContainer;

        public Mediator(
            ICommunicatorSerializer serializer,
            IClientTransportLayer clientTransportLayer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _clientTransportLayer = clientTransportLayer ?? throw new ArgumentNullException(nameof(clientTransportLayer));

            // TODO: Unsubscribe.
            clientTransportLayer.OnReceiveMessage += OnReceiveMessage_Client;
        }

        public Mediator(
            ICommunicatorSerializer serializer,
            IServerTransportLayer serverTransportLayer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serverTransportLayer = serverTransportLayer ?? throw new ArgumentNullException(nameof(serverTransportLayer));

            _isServer = true;
            // TODO: Unsubscribe.
            serverTransportLayer.OnReceiveMessage += OnReceiveMessage_Server;
            serverTransportLayer.OnClientConnected += OnClientConnected_Server;
            serverTransportLayer.OnClientDisconnected += OnClientDisconnected_Server;
        }

        public void SetServicesContainer(ServicesContainerRoot servicesContainer)
        {
            _servicesContainer = servicesContainer ?? throw new ArgumentNullException(nameof(servicesContainer));
        }

        public object Execute(ClientContext clientContext, string serviceName, string methodName, params object[] arguments)
        {
            if (_isServer && clientContext == null) throw new ArgumentNullException(nameof(clientContext));

            // Get service and method information.
            var servicInterfaceType = _servicesContainer.GetServiceInterfaceType(serviceName);
            var serviceMethod = servicInterfaceType.GetMethod(methodName);
            if (serviceMethod == null)
                throw new GenericRpcException($"Method with name {methodName} not found");

            // TODO: We don't need in recounting parameters per each call.
            var argumentTypes = serviceMethod.GetParameters().Select(x => x.ParameterType).ToArray();

            // TODO: In service can be many methods with the same name and arguments count.
            // Serialize arguments.
            if (argumentTypes.Length != arguments.Length)
                throw new GenericRpcException("Arguments count not equals");

            var argumentsBytes = new List<byte[]>(argumentTypes.Length);
            for (int i = 0; i < argumentTypes.Length; i++)
                argumentsBytes.Add(_serializer.Serialize(arguments[i], argumentTypes[i]));

            // Configure awaiter and send message.
            var messageId = Guid.NewGuid();
            var awaiter = new ResponseAwaiter();
            if (!_awaiterByMessageId.TryAdd(messageId, awaiter))
                throw new GenericRpcException($"Message with Id={messageId} already exists");

            var requestMessage = new RpcMessage(serviceName, methodName, messageId, RpcMessageType.Request, argumentsBytes.ToArray(), null);
            if (_isServer)
            {
                _serverTransportLayer.SendMessageAsync(requestMessage, clientContext).GetAwaiter().GetResult();
            }
            else
            {
                _clientTransportLayer.SendMessageAsync(requestMessage).GetAwaiter().GetResult();
            }

            var response = awaiter.GetResponse();
            _awaiterByMessageId.TryRemove(messageId, out _);

            // Deserialize and return result.
            var resultData = response.ResponseData == null ? null : _serializer.Deserialize(response.ResponseData, serviceMethod.ReturnType);

            return resultData;
        }

        private void OnClientConnected_Server(ClientContext clientContext)
        {
            _servicesContainer.RegisterServicesForClientContext(clientContext);
        }

        private void OnClientDisconnected_Server(ClientContext clientContext)
        {
            _servicesContainer.UnregisterServicesForClientContext(clientContext);
        }

        private void OnReceiveMessage_Client(RpcMessage message) => OnReceiveMessage(message, null);

        private void OnReceiveMessage_Server(RpcMessage message, ClientContext clientContext) => OnReceiveMessage(message, clientContext);

        private void OnReceiveMessage(RpcMessage message, ClientContext clientContext)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (_isServer && clientContext == null) throw new ArgumentNullException(nameof(clientContext));

            switch (message.MessageType)
            {
                case RpcMessageType.Request:
                    // Get service and method information.
                    var servicInterfaceType = _servicesContainer.GetServiceInterfaceType(message.ServiceName);
                    var serviceMethod = servicInterfaceType.GetMethod(message.MethodName);
                    if (serviceMethod == null)
                        throw new GenericRpcException($"Method with name {message.MethodName} not found");

                    // TODO: We don't need in recounting parameters per each call.
                    var argumentTypes = serviceMethod.GetParameters().Select(x => x.ParameterType).ToArray();

                    // TODO: In service can be many methods with the same name and arguments count.
                    // Deserialize arguments.
                    if (argumentTypes.Length != message.RequestData.Length)
                        throw new GenericRpcException("Arguments count not equals");

                    var arguments = new object[argumentTypes.Length];
                    for (int i = 0; i < argumentTypes.Length; i++)
                        arguments[i] = _serializer.Deserialize(message.RequestData[i], argumentTypes[i]);

                    // Invoke method and send result.
                    var service = _isServer
                        ? _servicesContainer.GetServicesContainer(clientContext).GetListenerService(servicInterfaceType)
                        : _servicesContainer.ClientContainer.GetListenerService(servicInterfaceType);
                    var result = serviceMethod.Invoke(service, arguments);
                    var resultData = result == null ? null : _serializer.Serialize(result, serviceMethod.ReturnType);

                    var responseMessage = new RpcMessage(message.ServiceName, message.MethodName, message.MessageId, RpcMessageType.Response, null, resultData);
                    if (_isServer)
                    {
                        _serverTransportLayer.SendMessageAsync(responseMessage, clientContext);
                    } else
                    {
                        _clientTransportLayer.SendMessageAsync(responseMessage);
                    }
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
                _resetEvent = new ManualResetEvent(false);
            }

            public void SetResponse(RpcMessage message)
            {
                if (message == null) throw new ArgumentNullException(nameof(message));
                if (_message != null) throw new GenericRpcException($"Message already setted");

                _message = message;
                _resetEvent.Set();
            }

            public RpcMessage GetResponse()
            {
                _resetEvent.WaitOne();
                return _message;
            }
        }
    }
}