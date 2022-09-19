using GenericRpc.Communicators;
using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.ServicesGeneration;
using GenericRpc.SocketTransport;
using GenericRpc.SocketTransport.Common;
using GenericRpc.Transport;
using GenericRpc.UnitTests.Common;
using GenericRpc.UnitTests.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GenericRpc.UnitTests
{
    [TestClass]
    public sealed class RpcTests
    {
        [TestMethod]
        public async Task ComunicatorBuildTest()
        {
            using var server = CreateServer();
            using var client = CreateClient();

            await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);

            var service = client.GetProxy<IExampleService>();

            await ExecuteWithDelayAsync(() => {
                var sum = service.Sum(123, 321);
                Assert.AreEqual(444, sum);
            });

            await ExecuteWithDelayAsync(() => { service.ShowMessage("Hello server!"); });
        }

        [TestMethod]
        public async Task ExecuteMethodAfterClientDisconnectedTest()
        {
            using var server = CreateServer();
            using var client = CreateClient();

            await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);

            var service = client.GetProxy<IExampleService>();
            await client.DisconnectAsync();

            await Assert.ThrowsExceptionAsync<GenericRpcSocketOfflineException>(async () =>
                await ExecuteWithDelayAsync(() => service.ShowMessage("Hello server!")));
        }

        [TestMethod]
        public async Task ExecuteMethodAfterServerStoppedTest()
        {
            using var server = CreateServer();
            using var client = CreateClient();

            await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            await client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);

            var service = client.GetProxy<IExampleService>();
            await server.StopAsync();

            await Assert.ThrowsExceptionAsync<MessageAwaitingCancelledGenericRpcException>(async () =>
                await ExecuteWithDelayAsync(() => service.ShowMessage("Hello server!")));
        }

        [TestMethod]
        public async Task ServerForceDisconnectClientTest()
        {
            using var server = CreateServer();
            using var client = CreateClient();

            await server.StartAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
            ClientContext clientContext = null;
            ClientConnected connectedHandler = (ClientContext context) => clientContext = context;
            bool isClientDisconnected = false;
            OnDisconnected disconnectedHandler = () => isClientDisconnected = true;
            server.OnClientConnected += connectedHandler;
            client.OnDisconnected += disconnectedHandler;

            try
            {
                await client.ConnectAsync(TestConfiguration.ServerIp, TestConfiguration.ServerPort);
                await Task.Delay(TestConfiguration.Delay);
                Assert.IsNotNull(clientContext);
                await server.DisconnectClientAsync(clientContext);

                // NOTE: wait 750ms because now client hasn't configuration and KeepAliveLoop launches each 500ms.
                await Task.Delay(TimeSpan.FromMilliseconds(750));
                Assert.AreEqual(true, isClientDisconnected);
            }
            finally
            {
                server.OnClientConnected -= connectedHandler;
                client.OnDisconnected -= disconnectedHandler;
            }
        }

        private static IClientCommunicator CreateClient() => new CommunicatorBuilder()
                .SetSerializer(new DefaultCommunicatorSerializer())
                .SetClientTransportLayer(new ClientSocketTransportLayer())
                .SetDependencyResolver(new MockDependencyResolver())
                .RegisterProxyService<IExampleService>()
                .BuildClient();

        private static IServerCommunicator CreateServer() => new CommunicatorBuilder()
                .SetSerializer(new DefaultCommunicatorSerializer())
                .SetServerTransportLayer(new ServerSocketTransportLayer())
                .SetDependencyResolver(new MockDependencyResolver())
                .RegisterListenerService<IExampleService, ServerExampleService>()
                .BuildServer();

        private static async Task ExecuteWithDelayAsync(Action action)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await Task.Run(action, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Assert.Fail("Task didn't complete in 5 seconds");
                throw;
            }
        }

        private class MockDependencyResolver : IListenerDependencyResolver
        {
            public object Resolve(Type type, ClientContext context)
            {
                Assert.AreEqual(type, typeof(ServerExampleService), "Unexpected type");

                return new ServerExampleService(context);
            }
        }
    }
}