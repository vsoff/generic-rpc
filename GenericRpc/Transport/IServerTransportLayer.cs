using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface IServerTransportLayer
    {
        event ClientConnected OnClientConnected;
        event ClientDisconnected OnClientDisconnected;

        Task StartAsync(string host, int port);
        Task StopAsync();

        Task SendMessageAsync(RpcMessage message, ClientContext context);
        event MessageReceivedWithClientId OnReceiveMessage;
    }
}