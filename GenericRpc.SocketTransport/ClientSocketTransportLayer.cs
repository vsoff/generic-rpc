using GenericRpc.SocketTransport.Common;
using GenericRpc.Transport;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport
{
    public class ClientSocketTransportLayer : BaseSocketTransportLayer, IClientTransportLayer
    {
        public event MessageReceived OnReceiveMessage;

        private Socket _serverSocket;

        public async Task SendMessageAsync(RpcMessage message)
        {
            await _serverSocket.SendMessageAsync(message);
        }

        public async Task ConnectAsync(string host, int port)
        {
            SetAliveOrThrow();

            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await _serverSocket.ConnectAsync(host, port);
                await Task.Factory.StartNew(async () => await CommunicateWithServerAsync());
            }
            catch (Exception exception)
            {
                await DisconnectAsync();
                throw new GenericRpcSocketTransportException("Error while accepting clients", exception);
            }
        }


        private async Task CommunicateWithServerAsync()
        {
            try
            {
                await foreach (var clientMessage in _serverSocket.StartReceiveMessagesAsync())
                OnReceiveMessage?.Invoke(clientMessage);
            }
            catch (Exception exception)
            {
                await DisconnectAsync();
                throw new GenericRpcSocketTransportException("Error communacating with server", exception);
            }
        }

        public Task DisconnectAsync()
        {
            if (!IsAlive())
                return Task.CompletedTask;

            var tempServerSocket = _serverSocket;
            if (tempServerSocket != null)
            {
                tempServerSocket.Disconnect(false);
                tempServerSocket.Dispose();
            }

            ResetAlive();
            return Task.CompletedTask;
        }
    }
}