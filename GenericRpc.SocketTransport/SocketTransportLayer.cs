using GenericRpc.Transport;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport
{

    public class SocketTransportLayer : ITransportLayer
    {
        private const int SocketBacklog = 10;

        public bool IsConnected => throw new NotImplementedException();

        public event Action<CommunicationErrorInfo> OnExceptionOccured;
        public event Action<RpcMessage> OnReceiveMessage;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public Task ConnectAsync(IPEndPoint endpoint)
        {
            throw new NotImplementedException();
        }

        public Task SendMessageAsync(RpcMessage message)
        {
            throw new NotImplementedException();
        }
        public async Task StartAsync(IPEndPoint endpoint)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Bind socket to endpoint and start listen.
                serverSocket.Bind(endpoint);
                serverSocket.Listen(SocketBacklog);

                // Accept each client socket.
                while (true)
                {
                    Socket clientSocket = null;
                    try
                    {
                        clientSocket = await serverSocket.AcceptAsync();
                        await Task.Factory.StartNew(() => HandleClientSocketAsync(clientSocket));
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while accepting client socket", exception));
                        DisconnectClient(clientSocket);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new GenericRpcSocketTransportException("Error while accepting clients", ex);
            }
        }

        private async Task HandleClientSocketAsync(Socket clientSocket)
        {
            try
            {
                // Communicate while client connected.
                await foreach (var clientMessage in clientSocket.StartReceivePackageMessagesAsync())
                    OnReceiveMessage?.Invoke(clientMessage);
            }
            catch (Exception exception)
            {
                OnExceptionOccured?.Invoke(new CommunicationErrorInfo("Error while communicating with client", exception));
                DisconnectClient(clientSocket);
            }
        }

        private static void DisconnectClient(Socket clientSocket)
        {
            clientSocket?.Disconnect(false);
            clientSocket?.Dispose();
        }
    }
}