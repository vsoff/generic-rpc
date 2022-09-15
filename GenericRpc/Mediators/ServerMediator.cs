﻿using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.Transport;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GenericRpc.Mediators
{
    internal sealed class ServerMediator : BaseMediator
    {
        // TODO: move to ResponseAwaitersContainer and remove with client disconnect.
        private readonly ConcurrentDictionary<Guid, ResponseAwaiter> _awaiterByMessageId = new ConcurrentDictionary<Guid, ResponseAwaiter>();
        private readonly IServerTransportLayer _serverTransportLayer;

        private bool _disposed = false;

        public ServerMediator(
            IServerTransportLayer serverTransportLayer,
            ICommunicatorSerializer serializer)
            : base(serializer)
        {
            _serverTransportLayer = serverTransportLayer ?? throw new ArgumentNullException(nameof(serverTransportLayer));
            serverTransportLayer.OnMessageReceived += OnReceiveMessage;
            serverTransportLayer.OnClientConnected += OnClientConnected;
            serverTransportLayer.OnClientDisconnected += OnClientDisconnected;
            _serverTransportLayer.OnServerShutdown += OnServerShutdown;
        }

        protected override object GetListenerService(ClientContext clientContext, Type serviceInterfaceType)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (clientContext == null) throw new ArgumentNullException(nameof(clientContext));
            return ServicesContainer.GetServicesContainer(clientContext).GetListenerService(serviceInterfaceType);
        }

        protected override async Task SendMessageAsync(ClientContext clientContext, RpcMessage message)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (clientContext == null) throw new ArgumentNullException(nameof(clientContext));
            await _serverTransportLayer.SendMessageAsync(message, clientContext);
        }

        protected override void CreateResponseAwaiter(ClientContext clientContext, Guid messageId)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (!_awaiterByMessageId.TryAdd(messageId, new ResponseAwaiter()))
                throw new GenericRpcException($"Message with Id={messageId} already exists");
        }

        protected override RpcMessage AwaitResponse(ClientContext clientContext, Guid messageId)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (!_awaiterByMessageId.TryGetValue(messageId, out var awaiter))
                throw new GenericRpcException($"Awaiter for message with Id={messageId} not found");

            var response = awaiter.GetResponse();
            _awaiterByMessageId.TryRemove(messageId, out _);
            return response;
        }

        protected override void SetResponse(ClientContext clientContext, RpcMessage responseMessage)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (_awaiterByMessageId.TryGetValue(responseMessage.MessageId, out var awaiter))
                awaiter.SetResponse(responseMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _serverTransportLayer.OnMessageReceived -= OnReceiveMessage;
                _serverTransportLayer.OnClientConnected -= OnClientConnected;
                _serverTransportLayer.OnClientDisconnected -= OnClientDisconnected;
                _serverTransportLayer.OnServerShutdown -= OnServerShutdown;
                // TODO: release all awaiters.
            }

            base.Dispose(disposing);
            _disposed = true;
        }

        private void OnServerShutdown()
        {
            // TODO: release all awaiters.
        }

        private void OnClientConnected(ClientContext clientContext)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            ServicesContainer.RegisterServicesForClientContext(clientContext);
        }

        private void OnClientDisconnected(ClientContext clientContext)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            ServicesContainer.UnregisterServicesForClientContext(clientContext);
        }
    }
}