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
        private MessageReceived _onMessageReceived;

        private CancellationTokenSource _clientCancellationTokenSource;
        private Socket _serverSocket;

        public event Action<CommunicationErrorInfo> OnExceptionOccured;

        public bool IsAlive => IsTransportAlive;

        public void SetRecieveMessageCallback(MessageReceived onMessageReceived)
        {
            if (_onMessageReceived != null) throw new GenericRpcSocketTransportException("Message recieve callback already setted");
            _onMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
        }

        public async Task SendMessageAsync(RpcMessage message)
        {
            await _serverSocket.SendMessageAsync(message);
        }

        public async Task ConnectAsync(string host, int port)
        {
            if (_onMessageReceived == null) throw new GenericRpcSocketTransportException("Message recieve callback not setted");

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
                    await _onMessageReceived.Invoke(clientMessage);
            }
            catch (TaskCanceledException)
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
            if (!IsTransportAlive)
                return Task.CompletedTask;

            if (!LockForStop())
                return Task.CompletedTask;

            try
            {
                _clientCancellationTokenSource?.Cancel();
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
            }

            return Task.CompletedTask;
        }
    }
}