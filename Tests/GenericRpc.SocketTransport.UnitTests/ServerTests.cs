using GenericRpc.SocketTransport.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport.UnitTests
{
    [TestClass]
    public class ServerTests
    {
        [TestMethod]
        public async Task StartTest()
        {
            var server = new ServerSocketTransportLayer();
            var serverErrors = new List<CommunicationErrorInfo>();
            server.OnExceptionOccured += serverErrors.Add;

            try
            {
                await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            finally
            {
                server.OnExceptionOccured -= serverErrors.Add;
            }

            Assert.AreEqual(0, serverErrors.Count);
        }

        [TestMethod]
        public async Task StartAndStopTest()
        {
            var server = new ServerSocketTransportLayer();
            var serverErrors = new List<CommunicationErrorInfo>();
            server.OnExceptionOccured += serverErrors.Add;

            try
            {
                await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await Task.Delay(TimeSpan.FromSeconds(1));
                await server.StopAsync();
            }
            finally
            {
                server.OnExceptionOccured -= serverErrors.Add;
            }

            Assert.AreEqual(0, serverErrors.Count);
        }

        [TestMethod]
        public async Task FewRestartsTest()
        {
            var server = new ServerSocketTransportLayer();
            var serverErrors = new List<CommunicationErrorInfo>();
            server.OnExceptionOccured += serverErrors.Add;

            try
            {
                foreach (var _ in Enumerable.Range(1, 5))
                {
                    await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                    await server.StopAsync();
                }
            }
            finally
            {
                server.OnExceptionOccured -= serverErrors.Add;
            }

            Assert.AreEqual(0, serverErrors.Count);
        }

        [TestMethod]
        public async Task TwoServersOnePortTest()
        {
            var server1 = new ServerSocketTransportLayer();
            var server2 = new ServerSocketTransportLayer();
            var server1Errors = new List<CommunicationErrorInfo>();
            var server2Errors = new List<CommunicationErrorInfo>();
            server1.OnExceptionOccured += server1Errors.Add;
            server2.OnExceptionOccured += server2Errors.Add;

            await server1.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await Assert.ThrowsExceptionAsync<GenericRpcSocketTransportException>(
                async () => await server2.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort));
        }
    }
}