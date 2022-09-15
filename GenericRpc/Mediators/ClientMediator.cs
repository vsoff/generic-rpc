using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.Transport;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GenericRpc.Mediators
{
    internal sealed class ClientMediator : BaseMediator
    {
        private readonly ConcurrentDictionary<Guid, ResponseAwaiter> _awaiterByMessageId = new ConcurrentDictionary<Guid, ResponseAwaiter>();

        private readonly IClientTransportLayer _clientTransportLayer;

        public ClientMediator(
            IClientTransportLayer clientTransportLayer,
            ICommunicatorSerializer serializer)
            : base(serializer)
        {
            _clientTransportLayer = clientTransportLayer ?? throw new ArgumentNullException(nameof(clientTransportLayer));
            clientTransportLayer.OnReceiveMessage += OnReceiveMessage;
        }

        protected override object GetListenerService(ClientContext clientContext, Type serviceInterfaceType)
            => ServicesContainer.ClientContainer.GetListenerService(serviceInterfaceType);

        protected override async Task SendMessageAsync(ClientContext clientContext, RpcMessage message)
            => await _clientTransportLayer.SendMessageAsync(message);

        protected override void CreateResponseAwaiter(ClientContext clientContext, Guid messageId)
        {
            if (!_awaiterByMessageId.TryAdd(messageId, new ResponseAwaiter()))
                throw new GenericRpcException($"Message with Id={messageId} already exists");
        }

        protected override RpcMessage AwaitResponse(ClientContext clientContext, Guid messageId)
        {
            if (!_awaiterByMessageId.TryGetValue(messageId, out var awaiter))
                throw new GenericRpcException($"Awaiter for message with Id={messageId} not found");

            var response = awaiter.GetResponse();
            _awaiterByMessageId.TryRemove(messageId, out _);
            return response;
        }

        protected override void SetResponse(ClientContext clientContext, RpcMessage responseMessage)
        {
            if (_awaiterByMessageId.TryGetValue(responseMessage.MessageId, out var awaiter))
                awaiter.SetResponse(responseMessage);
        }

        private async Task OnReceiveMessage(RpcMessage message) => await OnReceiveMessage(message, null);
    }
}