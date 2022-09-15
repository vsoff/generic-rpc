using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface IServerTransportLayer
    {
        Task StartAsync(string host, int port);
        Task StopAsync();

        Task SendMessageAsync(RpcMessage message, ClientContext context);

        void SetRecieveMessageCallback(MessageReceivedWithClientId onMessageReceived);
        void SetClientConnectedCallback(ClientConnected onClientConnected);
        void SetClientDisconnectedCallback(ClientDisconnected onClientDisconnected);
    }
}