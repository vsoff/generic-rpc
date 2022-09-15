using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.ServicesGeneration;
using GenericRpc.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GenericRpc.Mediators
{
    internal abstract class BaseMediator : IMediator
    {
        private readonly ICommunicatorSerializer _serializer;

        protected ServicesContainerRoot ServicesContainer { get; private set; }

        public BaseMediator(ICommunicatorSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public void SetServicesContainer(ServicesContainerRoot servicesContainer)
        {
            ServicesContainer = servicesContainer ?? throw new ArgumentNullException(nameof(servicesContainer));
        }

        public object Execute(ClientContext clientContext, string serviceName, string methodName, params object[] arguments)
            => ExecuteAsync(clientContext, serviceName, methodName, arguments).GetAwaiter().GetResult();

        private async Task<object> ExecuteAsync(ClientContext clientContext, string serviceName, string methodName, params object[] arguments)
        {
            // Get service and method information.
            var servicInterfaceType = ServicesContainer.GetServiceInterfaceType(serviceName);
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
            var requestMessage = new RpcMessage(serviceName, methodName, Guid.NewGuid(), RpcMessageType.Request, argumentsBytes.ToArray(), null);
            CreateResponseAwaiter(clientContext, requestMessage.MessageId);
            await SendMessageAsync(clientContext, requestMessage);

            // Await response.
            var responseMessage = AwaitResponse(clientContext, requestMessage.MessageId);

            // Deserialize and return result.
            var resultData = responseMessage.ResponseData == null ? null : _serializer.Deserialize(responseMessage.ResponseData, serviceMethod.ReturnType);

            return resultData;
        }

        protected abstract object GetListenerService(ClientContext clientContext, Type serviceInterfaceType);
        protected abstract Task SendMessageAsync(ClientContext clientContext, RpcMessage message);

        protected abstract void CreateResponseAwaiter(ClientContext clientContext, Guid messageId);
        protected abstract RpcMessage AwaitResponse(ClientContext clientContext, Guid messageId);
        protected abstract void SetResponse(ClientContext clientContext, RpcMessage responseMessage);

        protected async Task OnReceiveMessage(RpcMessage message, ClientContext clientContext)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            switch (message.MessageType)
            {
                case RpcMessageType.Request:
                    // Get service and method information.
                    var servicInterfaceType = ServicesContainer.GetServiceInterfaceType(message.ServiceName);
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
                    var service = GetListenerService(clientContext, servicInterfaceType);
                    var result = serviceMethod.Invoke(service, arguments);
                    var resultData = result == null ? null : _serializer.Serialize(result, serviceMethod.ReturnType);

                    var responseMessage = new RpcMessage(message.ServiceName, message.MethodName, message.MessageId, RpcMessageType.Response, null, resultData);
                    await SendMessageAsync(clientContext, responseMessage);
                    break;
                case RpcMessageType.Response:
                    SetResponse(clientContext, message);
                    break;
                default: throw new GenericRpcException($"Unknown message type {message.MessageType}");
            }
        }

        protected class ResponseAwaiter
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