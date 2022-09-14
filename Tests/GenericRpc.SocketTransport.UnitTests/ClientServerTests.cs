using GenericRpc.SocketTransport.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport.UnitTests
{
    [TestClass]
    public class ClientServerTests
    {
        [TestMethod]
        public async Task ServerDisconnectFirstTest()
        {
            var client = new ClientSocketTransportLayer();
            var server = new ServerSocketTransportLayer();
            var clientErrors = new List<CommunicationErrorInfo>();
            var serverErrors = new List<CommunicationErrorInfo>();
            client.OnExceptionOccured += clientErrors.Add;
            server.OnExceptionOccured += serverErrors.Add;

            try
            {
                await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await Task.Delay(1000);
                await server.StopAsync();
                await Task.Delay(1000);
                await client.DisconnectAsync();
            }
            finally
            {
                client.OnExceptionOccured -= clientErrors.Add;
                server.OnExceptionOccured -= serverErrors.Add;
            }

            Assert.AreEqual(0, clientErrors.Count);
            Assert.AreEqual(0, serverErrors.Count);
        }

        [TestMethod]
        public async Task ClientDisconnectFirstTest()
        {
            var client = new ClientSocketTransportLayer();
            var server = new ServerSocketTransportLayer();
            var clientErrors = new List<CommunicationErrorInfo>();
            var serverErrors = new List<CommunicationErrorInfo>();
            client.OnExceptionOccured += clientErrors.Add;
            server.OnExceptionOccured += serverErrors.Add;

            try
            {
                await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await Task.Delay(1000);
                await client.DisconnectAsync();
                await Task.Delay(1000);
                await server.StopAsync();
            }
            finally
            {
                client.OnExceptionOccured -= clientErrors.Add;
                server.OnExceptionOccured -= serverErrors.Add;
            }

            Assert.AreEqual(0, clientErrors.Count);
            Assert.AreEqual(0, serverErrors.Count);
        }
    }
}