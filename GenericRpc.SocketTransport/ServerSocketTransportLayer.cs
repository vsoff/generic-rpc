using GenericRpc.SocketTransport.Common;
using GenericRpc.Transport;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport
{
    public sealed class ServerSocketTransportLayer : BaseSocketTransportLayer, IServerTransportLayer
    {
        private const int SocketBacklog = 10;
        private readonly ConcurrentDictionary<ClientContext, Socket> _socketByClientId = new();

        private Socket _serverSocket;

        public event ClientConnected OnClientConnected;
        public event ClientDisconnected OnClientDisconnected;
        public event MessageReceivedWithClientId OnReceiveMessage;

        public event Action<CommunicationErrorInfo> OnExceptionOccured;

        public async Task SendMessageAsync(RpcMessage message, ClientContext context)
        {
            if (_socketByClientId.TryGetValue(context, out var clientSocket))
                await clientSocket.SendMessageAsync(message);
        }

        public async Task StartAsync(string host, int port)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            SetAliveOrThrow();

            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Bind socket to endpoint and start listen.
                _serverSocket.Bind(endpoint);
                _serverSocket.Listen(SocketBacklog);

                // Accept each client socket.
                await Task.Factory.StartNew(AcceptClientsAsync);
            }
            catch (Exception exception)
            {
                await StopAsync();
                throw new GenericRpcSocketTransportException("Error while accepting clients", exception);
            }
        }

        public Task StopAsync()
        {
            if (!IsAlive())
                return Task.CompletedTask;

            // TODO: Check client-server disconect ordering. Need synchronization.
            var tempServerSocket = _serverSocket;
            if (tempServerSocket != null)
            {
                tempServerSocket.Disconnect(false);
                tempServerSocket.Dispose();
            }

            foreach (var clientId in _socketByClientId.Keys)
                DisconnectClient(clientId);

            ResetAlive();
            return Task.CompletedTask;
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var clientContext = new ClientContext(Guid.NewGuid());

                try
                {
                    Socket clientSocket = await _serverSocket.AcceptAsync();
                    if (!_socketByClientId.TryAdd(clientContext, clientSocket))
                        throw new GenericRpcSocketTransportException("Socket not added");

                    OnClientConnected.Invoke(clientContext);
                    await Task.Run(async () => await CommunicateWithClientAsync(clientContext));
                }
                catch (Exception exception)
                {
                    OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while accepting client socket", exception));
                    DisconnectClient(clientContext);
                }
            }
        }

        private async Task CommunicateWithClientAsync(ClientContext context)
        {
            try
            {
                if (!_socketByClientId.TryGetValue(context, out var clientSocket))
                    return;

                await foreach (var clientMessage in clientSocket.StartReceiveMessagesAsync())
                    OnReceiveMessage?.Invoke(clientMessage, context);
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while communicating with client", exception));
                DisconnectClient(context);
            }
        }

        private void DisconnectClient(ClientContext context)
        {
            if (!_socketByClientId.TryRemove(context, out var clientSocket))
                return;

            clientSocket?.Disconnect(false);
            clientSocket?.Dispose();
            OnClientDisconnected.Invoke(context);
        }
    }
}