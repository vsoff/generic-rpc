using GenericRpc.Exceptions;
using GenericRpc.Transport;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GenericRpc.Mediators
{
    internal sealed class AwaitingMessagesContainerRoot : IDisposable
    {
        private readonly ConcurrentDictionary<ClientContext, Container> _containerByContext = new ConcurrentDictionary<ClientContext, Container>();
        private readonly Container _clientContainer;
        private bool _disposed = false;

        public IContainer ClientContainer => _clientContainer;

        public AwaitingMessagesContainerRoot()
        {
            _clientContainer = new Container();
        }

        public IContainer GetServicesContainer(ClientContext context)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!_containerByContext.TryGetValue(context, out var container))
                throw new GenericRpcException($"Services container for context with id = {context.Id} isn't registered");

            return container;
        }

        public void RegisterServicesForClientContext(ClientContext context)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (!_containerByContext.TryAdd(context, new Container()))
                throw new GenericRpcException("Error while register services for client");
        }

        public void UnregisterServicesForClientContext(ClientContext context)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (!_containerByContext.TryRemove(context, out var removedContainer))
                throw new GenericRpcException("Error while unregister services for client");

            removedContainer.Dispose();
        }

        public void Clear()
        {
            _clientContainer.Clear();

            var containers = _containerByContext.Values;
            _containerByContext.Clear();

            foreach (var container in containers)
                container.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _clientContainer.Dispose();

            var containers = _containerByContext.Values;
            _containerByContext.Clear();

            foreach (var container in containers)
                container.Dispose();

            _disposed = true;
        }

        public interface IContainer
        {
            void CreateResponseAwaiter(Guid messageId);
            RpcMessage AwaitResponse(Guid messageId);
            void SetResponse(RpcMessage responseMessage);
        }

        private sealed class Container : IContainer, IDisposable
        {
            private readonly ConcurrentDictionary<Guid, ResponseAwaiter> _awaiterByMessageId = new ConcurrentDictionary<Guid, ResponseAwaiter>();

            private bool _disposed = false;

            public void CreateResponseAwaiter(Guid messageId)
            {
                if (_disposed) throw new ObjectDisposedException(GetType().FullName);
                if (!_awaiterByMessageId.TryAdd(messageId, new ResponseAwaiter()))
                    throw new GenericRpcException($"Message with Id={messageId} already exists");
            }

            public RpcMessage AwaitResponse(Guid messageId)
            {
                if (_disposed) throw new ObjectDisposedException(GetType().FullName);
                if (!_awaiterByMessageId.TryGetValue(messageId, out var awaiter))
                    throw new GenericRpcException($"Awaiter for message with Id={messageId} not found");

                var response = awaiter.GetResponse();
                if (_awaiterByMessageId.TryRemove(messageId, out var removedAwaiter))
                    removedAwaiter.Dispose();

                return response;
            }

            public void SetResponse(RpcMessage responseMessage)
            {
                if (_disposed) throw new ObjectDisposedException(GetType().FullName);
                if (_awaiterByMessageId.TryGetValue(responseMessage.MessageId, out var awaiter))
                    awaiter.SetResponse(responseMessage);
            }

            public void Clear()
            {
                var awaiters = _awaiterByMessageId.Values;
                _awaiterByMessageId.Clear();

                foreach (var awaiter in awaiters)
                    awaiter.Dispose();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                Clear();

                _disposed = true;
            }
        }

        private sealed class ResponseAwaiter :IDisposable
        {
            private readonly ManualResetEvent _resetEvent;

            private RpcMessage _message;
            private bool _canceled;
            private bool _disposed;

            public ResponseAwaiter()
            {
                _resetEvent = new ManualResetEvent(false);
            }

            public void SetResponse(RpcMessage message)
            {
                if (_disposed) throw new ObjectDisposedException(GetType().FullName);
                if (message == null) throw new ArgumentNullException(nameof(message));
                if (_message != null) throw new GenericRpcException($"Message already setted");

                _message = message;
                _resetEvent.Set();
            }

            public void AbortAwait()
            {
                if (_disposed) throw new ObjectDisposedException(GetType().FullName);

                _canceled = true;
                _resetEvent.Set();
            }

            public RpcMessage GetResponse()
            {
                if (_disposed) throw new ObjectDisposedException(GetType().FullName);

                _resetEvent.WaitOne();
                if (_canceled)
                    throw new MessageAwaitingCancelledGenericRpcException("Message awaiting was cancelled");

                return _message;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                AbortAwait();
                _resetEvent.Dispose();

                _disposed = true;
            }
        }
    }
}