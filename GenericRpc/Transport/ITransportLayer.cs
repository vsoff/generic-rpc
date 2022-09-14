using System;
using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public delegate void ClientConnected(ClientContext context);
    public delegate void ClientDisconnected(ClientContext context);
    public delegate void MessageReceived(RpcMessage message);
    public delegate void MessageReceivedWithClientId(RpcMessage message, ClientContext context);

    public interface IClientTransportLayer
    {
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();
        Task SendMessageAsync(RpcMessage message);
        event MessageReceived OnReceiveMessage;
    }

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