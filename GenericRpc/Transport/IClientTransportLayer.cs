using System;
using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface IClientTransportLayer : IDisposable
    {
        bool IsAlive { get; }

        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();

        Task SendMessageAsync(RpcMessage message);
        event MessageReceived OnMessageReceived;
        event OnDisconnected OnDisconnected;
    }
}