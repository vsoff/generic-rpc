using GenericRpc.SocketTransport.Common;
using GenericRpc.Transport;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport
{
    public class ClientSocketTransportLayer : BaseSocketTransportLayer, IClientTransportLayer
    {
        private bool _disposed;


        private CancellationTokenSource _clientCancellationTokenSource;
        private Socket _serverSocket;

        public event Action<CommunicationErrorInfo> OnExceptionOccured;
        public event MessageReceived OnMessageReceived;
        public event OnDisconnected OnDisconnected;

        public bool IsAlive => IsTransportAlive;

        public async Task SendMessageAsync(RpcMessage message)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (!IsAlive) throw new GenericRpcSocketOfflineException();
            await _serverSocket.SendMessageAsync(message);
        }

        public async Task ConnectAsync(string host, int port)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            SetAliveOrThrow();

            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientCancellationTokenSource = new CancellationTokenSource();

                await _serverSocket.ConnectAsync(host, port);
                await Task.Factory.StartNew(async () => await CommunicateWithServerAsync(_clientCancellationTokenSource.Token));
            }
            catch (Exception exception)
            {
                await DisconnectAsync();
                throw new GenericRpcSocketTransportException("Error while accepting clients", exception);
            }
        }


        private async Task CommunicateWithServerAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var clientMessage in _serverSocket.StartReceiveMessagesAsync(cancellationToken))
                    await OnMessageReceived?.Invoke(clientMessage);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                await DisconnectAsync();

                if (!cancellationToken.IsCancellationRequested)
                {
                    OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error communicating with server", exception));
                    throw new GenericRpcSocketTransportException("Error communacating with server", exception);
                }
            }
        }

        public Task DisconnectAsync()
        {
            Disconnect();
            return Task.CompletedTask;
        }

        public void Disconnect()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            if (!IsTransportAlive || !LockForStop())
                return;

            try
            {
                _serverSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                _clientCancellationTokenSource?.Cancel();
                _clientCancellationTokenSource?.Dispose();
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while call cancelation token while disconnecting", exception));
            }
            finally
            {
                _clientCancellationTokenSource = null;
            }

            try
            {
                var tempServerSocket = _serverSocket;
                _serverSocket = null;
                if (tempServerSocket != null)
                {
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
                OnDisconnected?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                if (IsAlive)
                    Disconnect();
            }
            catch
            {
            }

            _disposed = true;
        }
    }
}