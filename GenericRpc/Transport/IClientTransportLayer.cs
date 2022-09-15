using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface IClientTransportLayer
    {
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();

        Task SendMessageAsync(RpcMessage message);
        void SetRecieveMessageCallback(MessageReceived onMessageReceived);
    }
}