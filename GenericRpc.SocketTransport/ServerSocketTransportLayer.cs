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
        private bool _disposed;

        private const int SocketBacklog = 10;
        private readonly ConcurrentDictionary<ClientContext, Socket> _socketByClientId = new();

        private CancellationTokenSource _serverCancellationTokenSource;
        private Socket _serverSocket;

        public event Action<CommunicationErrorInfo> OnExceptionOccured;

        public event MessageReceivedWithClientId OnMessageReceived;
        public event ClientConnected OnClientConnected;
        public event ClientDisconnected OnClientDisconnected;
        public event ServerShutdown OnServerShutdown;

        public bool IsAlive => IsTransportAlive;

        public async Task DisconnectClientAsync(ClientContext context)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (!IsAlive) throw new GenericRpcSocketOfflineException();
            if (!_socketByClientId.TryGetValue(context, out var clientSocket))
                throw new GenericRpcSocketTransportException($"Client with id {context.Id} not connected");
            
            await Task.Run(() => SafeDisconnectClient(context));
        }

        public async Task SendMessageAsync(RpcMessage message, ClientContext context)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (!IsAlive) throw new GenericRpcSocketOfflineException();
            if (_socketByClientId.TryGetValue(context, out var clientSocket))
                await clientSocket.SendMessageAsync(message);
        }

        public async Task StartAsync(string host, int port)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

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
                    catch (OperationCanceledException)
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
            Stop();
            return Task.CompletedTask;
        }

        public void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (!IsTransportAlive || !LockForStop())
                return;
            
            try
            {
                _serverSocket?.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

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
                OnServerShutdown?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                if (IsAlive)
                    Stop();
            }
            catch
            {
            }

            _disposed = true;
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

                    OnClientConnected?.Invoke(clientContext);
                    await Task.Factory.StartNew(async () => await CommunicateWithClientAsync(clientContext, cancellationToken));
                }
                catch (OperationCanceledException)
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
                    await OnMessageReceived?.Invoke(clientMessage, context);
                }
            }
            catch (OperationCanceledException)
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
                OnClientDisconnected?.Invoke(context);
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo($"Error while disconnect client socket. Id = {context.Id}", exception));
            }
        }
    }
}