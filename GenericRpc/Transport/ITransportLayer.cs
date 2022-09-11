using System;
using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public interface ITransportLayer
    {
        event Action<RpcMessage> OnReceiveMessage;
        event Action OnConnected;
        event Action OnDisconnected;
        bool IsConnected { get; }
        void Start(string ip, ushort port);
        void Connect(string ip, ushort port);
        Task SendMessageAsync(RpcMessage message);
    }
}