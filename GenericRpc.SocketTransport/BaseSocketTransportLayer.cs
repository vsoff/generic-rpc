using GenericRpc.SocketTransport.Common;

namespace GenericRpc.SocketTransport
{
    public abstract class BaseSocketTransportLayer
    {
        private readonly object _startLock = new();
        private bool _isAlive = false;

        protected void SetAliveOrThrow()
        {
            lock (_startLock)
            {
                if (_isAlive)
                    throw new GenericRpcSocketTransportException("Socket already in use");

                _isAlive = true;
            }
        }

        protected void ResetAlive()
        {
            lock (_startLock)
            {
                _isAlive = false;
            }
        }

        protected bool IsAlive()
        {
            lock (_startLock)
            {
                return _isAlive;
            }
        }
    }
}