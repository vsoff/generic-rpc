using System;
using System.Net;
using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface ITransportLayer
    {
        event Action<RpcMessage> OnReceiveMessage;
        event Action OnConnected;
        event Action OnDisconnected;
        bool IsConnected { get; }
        Task StartAsync(IPEndPoint endpoint);
        Task ConnectAsync(IPEndPoint endpoint);
        Task SendMessageAsync(RpcMessage message);
    }
}