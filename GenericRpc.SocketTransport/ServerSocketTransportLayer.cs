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
            if (!IsAlive())
                return Task.CompletedTask;

            SetStoppingOrThrow();

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
                cancellationToken.ThrowIfCancellationRequested();
                var clientContext = new ClientContext(Guid.NewGuid());

                try
                {
                    Socket clientSocket = await _serverSocket.AcceptAsync();
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_socketByClientId.TryAdd(clientContext, clientSocket))
                        throw new GenericRpcSocketTransportException("Socket not added");

                    OnClientConnected?.Invoke(clientContext);
                    await Task.Run(async () => await CommunicateWithClientAsync(clientContext, cancellationToken));
                }
                catch (TaskCanceledException)
                {
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
                    OnReceiveMessage?.Invoke(clientMessage, context);
                }
            }
            catch (TaskCanceledException)
            {
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
                OnClientDisconnected?.Invoke(context);
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo($"Error while disconnect client socket. Id = {context.Id}", exception));
            }
        }
    }
}