using GenericRpc.SocketTransport.Common;
using System;

namespace GenericRpc.SocketTransport
{
    public abstract class BaseSocketTransportLayer
    {
        protected static readonly TimeSpan KeepAlivePeriod = TimeSpan.FromMilliseconds(500);
        protected bool IsTransportAlive => _isAlive;

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

        protected bool LockForStop()
        {
            lock (_stopLock)
            {
                if (_isStopping)
                    return false;

                _isStopping = true;
            }

            return true;
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