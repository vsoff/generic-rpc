using System;
using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface IServerTransportLayer : IDisposable
    {
        bool IsAlive { get; }

        Task StartAsync(string host, int port);
        Task StopAsync();

        Task DisconnectClientAsync(ClientContext context);
        Task SendMessageAsync(RpcMessage message, ClientContext context);

        event MessageReceivedWithClientId OnMessageReceived;
        event ClientConnected OnClientConnected;
        event ClientDisconnected OnClientDisconnected;
        event ServerShutdown OnServerShutdown;
    }
}