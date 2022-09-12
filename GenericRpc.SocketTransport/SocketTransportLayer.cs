using GenericRpc.Transport;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport
{
    public class SocketTransportLayer : ITransportLayer
    {
        private const int SocketBacklog = 10;
        private readonly object _startLock = new();
        private readonly ConcurrentDictionary<Guid, Socket> _socketByClientId = new ConcurrentDictionary<Guid, Socket>();

        private Socket _serverSocket;
        private bool _isAlive = false;

        public event Action<CommunicationErrorInfo> OnExceptionOccured;
        public event MessageReceived OnReceiveMessage;

        public async Task SendMessageAsync(RpcMessage message, Guid? clientId)
        {
            if (clientId.HasValue)
            {
                if (!_socketByClientId.TryGetValue(clientId.Value, out var clientSocket))
                    await clientSocket.SendMessageAsync(message);
            }
            else
            {
                await _serverSocket.SendMessageAsync(message);
            }
        }

        public async Task ConnectAsync(IPEndPoint endpoint)
        {
            SetLockOrThrow();

            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await _serverSocket.ConnectAsync(endpoint);

                await foreach (var clientMessage in _serverSocket.StartReceiveMessagesAsync())
                    OnReceiveMessage?.Invoke(clientMessage, null);
            }
            catch (Exception exception)
            {
                throw new GenericRpcSocketTransportException("Error while accepting clients", exception);
            }
            finally
            {
                _serverSocket?.Disconnect(false);
                _serverSocket?.Dispose();
                _isAlive = false;
            }
        }

        public async Task StartAsync(IPEndPoint endpoint)
        {
            SetLockOrThrow();

            try
            {
                Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Bind socket to endpoint and start listen.
                serverSocket.Bind(endpoint);
                serverSocket.Listen(SocketBacklog);

                // Accept each client socket.
                while (true)
                {
                    var clientId = Guid.NewGuid();

                    try
                    {
                        Socket clientSocket = await serverSocket.AcceptAsync();
                        if (!_socketByClientId.TryAdd(clientId, clientSocket))
                            throw new GenericRpcSocketTransportException("Socket not added");

                        await Task.Factory.StartNew(async () => await CommunicateWithClientAsync(clientId));
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while accepting client socket", exception));
                        DisconnectClient(clientId);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new GenericRpcSocketTransportException("Error while accepting clients", exception);
            }
            finally
            {
                _isAlive = false;
            }
        }

        private async Task CommunicateWithClientAsync(Guid clientId)
        {
            try
            {
                if (!_socketByClientId.TryGetValue(clientId, out var clientSocket))
                    return;

                // Communicate while client connected.
                await foreach (var clientMessage in clientSocket.StartReceiveMessagesAsync())
                    OnReceiveMessage?.Invoke(clientMessage, clientId);
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while communicating with client", exception));
                DisconnectClient(clientId);
            }
        }

        private void DisconnectClient(Guid clientId)
        {
            if (!_socketByClientId.TryRemove(clientId, out var clientSocket))
                return;

            clientSocket?.Disconnect(false);
            clientSocket?.Dispose();
        }

        private void SetLockOrThrow()
        {
            lock (_startLock)
            {
                if (_isAlive)
                    throw new GenericRpcSocketTransportException("Socket already in use");

                _isAlive = true;
            }
        }
    }
}