namespace GenericRpc.SocketTransport.Common
{
    public sealed class GenericRpcSocketOfflineException : GenericRpcSocketTransportException
    {
        public GenericRpcSocketOfflineException() : base("Socket is not alive")
        {
        }
    }
}