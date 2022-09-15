using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public delegate void ClientConnected(ClientContext context);
    public delegate void ClientDisconnected(ClientContext context);
    public delegate Task MessageReceived(RpcMessage message);
    public delegate Task MessageReceivedWithClientId(RpcMessage message, ClientContext context);
}
