using GenericRpc.SocketTransport.Common;
using GenericRpc.Transport;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport
{
    public sealed class ServerSocketTransportLayer : BaseSocketTransportLayer, IServerTransportLayer
    {
        private const int SocketBacklog = 10;
        private readonly ConcurrentDictionary<ClientContext, Socket> _socketByClientId = new();

        private CancellationTokenSource _serverCancellationTokenSource;
        private Socket _serverSocket;

        private ClientConnected _onClientConnected;
        private ClientDisconnected _onClientDisconnected;
        private MessageReceivedWithClientId _onMessageReceived;

        public event Action<CommunicationErrorInfo> OnExceptionOccured;

        public bool IsAlive => IsTransportAlive;

        public void SetClientConnectedCallback(ClientConnected onClientConnected)
        {
            if (_onClientConnected != null) throw new GenericRpcSocketTransportException("Client connected callback already setted");
            _onClientConnected = onClientConnected ?? throw new ArgumentNullException(nameof(onClientConnected));
        }

        public void SetClientDisconnectedCallback(ClientDisconnected onClientDisconnected)
        {
            if (_onClientDisconnected != null) throw new GenericRpcSocketTransportException("Client disconnected callback already setted");
            _onClientDisconnected = onClientDisconnected ?? throw new ArgumentNullException(nameof(onClientDisconnected));
        }

        public void SetRecieveMessageCallback(MessageReceivedWithClientId onMessageReceived)
        {
            if (_onMessageReceived != null) throw new GenericRpcSocketTransportException("Receive message callback already setted");
            _onMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
        }

        public async Task SendMessageAsync(RpcMessage message, ClientContext context)
        {
            if (_socketByClientId.TryGetValue(context, out var clientSocket))
                await clientSocket.SendMessageAsync(message);
        }

        public async Task StartAsync(string host, int port)
        {
            if (_onMessageReceived == null) throw new GenericRpcSocketTransportException("Message recieve callback not setted");
            if (_onClientConnected == null) throw new GenericRpcSocketTransportException("Client connected callback not setted");
            if (_onClientDisconnected == null) throw new GenericRpcSocketTransportException("Client disconnected callback not setted");

            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            SetAliveOrThrow();

            try
            {
                // Start socket.
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverCancellationTokenSource = new CancellationTokenSource();
                var token = _serverCancellationTokenSource.Token;

                _serverSocket.Bind(endpoint);
                _serverSocket.Listen(SocketBacklog);

                // Accept each client socket.
                await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await AcceptClientsAsync(token);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        await StopAsync();
                        if (!token.IsCancellationRequested)
                        {
                            OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while start accepting client sockets", exception));
                            throw;
                        }
                    }
                });
            }
            catch (Exception exception)
            {
                await StopAsync();
                throw new GenericRpcSocketTransportException("Error while starting socket", exception);
            }
        }

        public Task StopAsync()
        {
            if (!IsTransportAlive)
                return Task.CompletedTask;

            if (!LockForStop())
                return Task.CompletedTask;

            try
            {
                _serverCancellationTokenSource?.Cancel();
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while call cancelation token while stopping", exception));
            }
            finally
            {
                _serverCancellationTokenSource = null;
            }

            try
            {
                foreach (var clientId in _socketByClientId.Keys)
                    SafeDisconnectClient(clientId);

                var tempServerSocket = _serverSocket;
                _serverSocket = null;

                if (tempServerSocket != null)
                {
                    if (tempServerSocket.Connected)
                        tempServerSocket.Disconnect(false);

                    tempServerSocket.Dispose();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                ResetAlive();
                ResetStopping();
            }

            return Task.CompletedTask;
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var clientContext = new ClientContext(Guid.NewGuid());

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Socket clientSocket = await _serverSocket.AcceptAsync();
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_socketByClientId.TryAdd(clientContext, clientSocket))
                        throw new GenericRpcSocketTransportException("Socket not added");

                    _onClientConnected.Invoke(clientContext);
                    await Task.Factory.StartNew(async () => await CommunicateWithClientAsync(clientContext, cancellationToken));
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while accepting client socket", exception));

                    SafeDisconnectClient(clientContext);
                }
            }
        }

        private async Task CommunicateWithClientAsync(ClientContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (!_socketByClientId.TryGetValue(context, out var clientSocket))
                    return;

                await foreach (var clientMessage in clientSocket.StartReceiveMessagesAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _onMessageReceived.Invoke(clientMessage, context);
                }
            }
            catch (TaskCanceledException)
            {
                SafeDisconnectClient(context);
            }
            catch (Exception exception)
            {
                if (!cancellationToken.IsCancellationRequested)
                    OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while communicating with client", exception));

                SafeDisconnectClient(context);
            }
        }

        private void SafeDisconnectClient(ClientContext context)
        {
            try
            {
                if (!_socketByClientId.TryRemove(context, out var clientSocket))
                    return;

                clientSocket?.Disconnect(false);
                clientSocket?.Dispose();
                _onClientDisconnected.Invoke(context);
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo($"Error while disconnect client socket. Id = {context.Id}", exception));
            }
        }
    }
}