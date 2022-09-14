using GenericRpc.SocketTransport.Common;

namespace GenericRpc.SocketTransport
{
    public abstract class BaseSocketTransportLayer
    {
        private readonly object _startLock = new();
        private readonly object _stopLock = new();
        private bool _isAlive = false;
        private bool _isStopping = false;

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

        protected void SetStoppingOrThrow()
        {
            lock (_stopLock)
            {
                if (_isStopping)
                    throw new GenericRpcSocketTransportException("Socket already stopping");

                _isStopping = true;
            }
        }

        protected void ResetStopping()
        {
            lock (_stopLock)
            {
                _isStopping = false;
            }
        }
    }
}