using GenericRpc.SocketTransport.UnitTests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport.UnitTests
{
    [TestClass]
    public class ClientServerTests
    {
        [TestMethod]
        public async Task ServerDisconnectFirstTest()
        {
            using var clientContainer = new ClientTestContainer();
            using var serverContainer = new ServerTestContainer();

            await serverContainer.Server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await clientContainer.Client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await Task.Delay(TestConfiguration.Delay);
            await serverContainer.Server.StopAsync();
            await Task.Delay(TestConfiguration.Delay);
            await clientContainer.Client.DisconnectAsync();

            Assert.AreEqual(0, clientContainer.Errors.Count);
            Assert.AreEqual(0, serverContainer.Errors.Count);
        }

        [TestMethod]
        public async Task ClientDisconnectFirstTest()
        {
            using var clientContainer = new ClientTestContainer();
            using var serverContainer = new ServerTestContainer();

            await serverContainer.Server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await clientContainer.Client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await Task.Delay(TestConfiguration.Delay);
            await clientContainer.Client.DisconnectAsync();
            await Task.Delay(TestConfiguration.Delay);
            await serverContainer.Server.StopAsync();

            Assert.AreEqual(0, clientContainer.Errors.Count);
            Assert.AreEqual(0, serverContainer.Errors.Count);
        }
    }
}