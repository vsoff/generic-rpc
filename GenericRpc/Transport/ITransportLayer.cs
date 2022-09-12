using System;
using System.Net;
using System.Threading.Tasks;

namespace GenericRpc.Transport
{
    public delegate void MessageReceived(RpcMessage message, Guid? clientId);

    public interface ITransportLayer
    {
        Task SendMessageAsync(RpcMessage message, Guid? clientId);
        event MessageReceived OnReceiveMessage;
    }
}