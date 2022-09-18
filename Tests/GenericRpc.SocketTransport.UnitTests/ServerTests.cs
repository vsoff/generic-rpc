using GenericRpc.SocketTransport.Common;
using GenericRpc.SocketTransport.UnitTests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport.UnitTests
{
    [TestClass]
    public sealed class ServerTests
    {
        [TestMethod]
        public async Task StartAndStopTest()
        {
            using var serverContainer = new ServerTestContainer();

            await serverContainer.Server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await Task.Delay(TestConfiguration.Delay);
            Assert.AreEqual(0, serverContainer.Errors.Count);

            await serverContainer.Server.StopAsync();

            Assert.AreEqual(0, serverContainer.Errors.Count);
        }

        [TestMethod]
        public async Task FewRestartsWithDelayTest()
        {
            using var serverContainer = new ServerTestContainer();

            foreach (var _ in Enumerable.Range(1, 5))
            {
                await serverContainer.Server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await Task.Delay(TestConfiguration.Delay);
                await serverContainer.Server.StopAsync();
                await Task.Delay(TestConfiguration.Delay);
            }

            Assert.AreEqual(0, serverContainer.Errors.Count);
        }

        [TestMethod]
        public async Task FewRestartsTest()
        {
            using var serverContainer = new ServerTestContainer();

            foreach (var _ in Enumerable.Range(1, 5))
            {
                await serverContainer.Server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await serverContainer.Server.StopAsync();
            }

            Assert.AreEqual(0, serverContainer.Errors.Count);
        }

        [TestMethod]
        public async Task TwoServersOnePortTest()
        {
            using var server1Container = new ServerTestContainer();
            using var server2Container = new ServerTestContainer();

            await server1Container.Server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await Assert.ThrowsExceptionAsync<GenericRpcSocketTransportException>(
                async () => await server2Container.Server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort));

            Assert.AreEqual(0, server1Container.Errors.Count);
            Assert.AreEqual(0, server2Container.Errors.Count);
        }
    }
}