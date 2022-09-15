using GenericRpc.SocketTransport.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport.UnitTests.Common
{
    internal class ClientTestContainer : IDisposable
    {
        public ClientSocketTransportLayer Client { get; }
        public IReadOnlyList<CommunicationErrorInfo> Errors => _errors;
        private readonly List<CommunicationErrorInfo> _errors;

        public ClientTestContainer()
        {
            Client = new ClientSocketTransportLayer();
            _errors = new List<CommunicationErrorInfo>();
            Client.OnExceptionOccured += _errors.Add;

            Client.SetRecieveMessageCallback((_) => Task.CompletedTask);
        }

        public void Dispose()
        {
            Client.OnExceptionOccured -= _errors.Add;
            if (Client.IsAlive)
                Client.DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}