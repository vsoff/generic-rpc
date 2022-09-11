using GenericRpc.Transport;
using System;
using System.Threading.Tasks;

namespace GenericRpc.Socket
{
    public class SocketTransportLayer : ITransportLayer
    {
        public bool IsConnected => throw new NotImplementedException();

        public event Action<RpcMessage> OnReceiveMessage;
        public event Action OnDisconnected;
        public event Action OnConnected;

        public void Connect(string ip, ushort port)
        {
            throw new NotImplementedException();
        }

        public Task SendMessageAsync(RpcMessage message)
        {
            throw new NotImplementedException();
        }

        public void Start(string ip, ushort port)
        {
            throw new NotImplementedException();
        }
    }
}