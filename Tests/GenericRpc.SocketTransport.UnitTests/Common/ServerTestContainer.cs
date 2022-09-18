using GenericRpc.SocketTransport.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport.UnitTests.Common
{
    internal sealed class ServerTestContainer : IDisposable
    {
        public ServerSocketTransportLayer Server { get; }
        public IReadOnlyList<CommunicationErrorInfo> Errors => _errors;
        private readonly List<CommunicationErrorInfo> _errors;

        public ServerTestContainer()
        {
            Server = new ServerSocketTransportLayer();
            _errors = new List<CommunicationErrorInfo>();
            Server.OnExceptionOccured += _errors.Add;
        }

        public void Dispose()
        {
            Server.OnExceptionOccured -= _errors.Add;
            Server.Dispose();
        }
    }
}