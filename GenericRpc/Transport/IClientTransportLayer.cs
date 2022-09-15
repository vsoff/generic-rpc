using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface IClientTransportLayer
    {
        bool IsAlive { get; }

        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();

        Task SendMessageAsync(RpcMessage message);
        void SetRecieveMessageCallback(MessageReceived onMessageReceived);
    }
}