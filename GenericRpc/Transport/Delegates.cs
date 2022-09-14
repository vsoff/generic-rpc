namespace GenericRpc.Transport
{
    public delegate void ClientConnected(ClientContext context);
    public delegate void ClientDisconnected(ClientContext context);
    public delegate void MessageReceived(RpcMessage message);
    public delegate void MessageReceivedWithClientId(RpcMessage message, ClientContext context);
}
