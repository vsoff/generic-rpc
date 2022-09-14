using GenericRpc.Serialization;
using GenericRpc.SocketTransport;
using GenericRpc.UnitTests.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GenericRpc.UnitTests
{
    [TestClass]
    public class RpcTests
    {
        private const string _serverIp = "127.0.0.1";
        private const ushort _serverPort = 21337;

        [TestMethod]
        public async Task ComunicatorBuildTest()
        {
            var serverCommunicator = new CommunicatorBuilder()
                .SetSerializer(new DefaultCommunicatorSerializer())
                .SetServerTransportLayer(new ServerSocketTransportLayer())
                .SetDependencyResolver(new MockDependencyResolver())
                .RegisterListenerService<IExampleService, ServerExampleService>()
                .BuildServer();

            var clientCommunicator = new CommunicatorBuilder()
                .SetSerializer(new DefaultCommunicatorSerializer())
                .SetClientTransportLayer(new ClientSocketTransportLayer())
                .SetDependencyResolver(new MockDependencyResolver())
                .RegisterProxyService<IExampleService>()
                .BuildClient();

            await serverCommunicator.StartAsync(_serverIp, _serverPort);
            await clientCommunicator.ConnectAsync(_serverIp, _serverPort);

            var service = clientCommunicator.GetProxy<IExampleService>();

            await ExecuteWithDelayAsync(() => {
                var sum = service.Sum(123, 321);
                Assert.AreEqual(444, sum);
            });
            await ExecuteWithDelayAsync(() => {
                service.ShowMessage("Hello server!");
            });
        }

        private static async Task ExecuteWithDelayAsync(Action action)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await Task.Run(action, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                Assert.Fail("Task didn't complete in 5 seconds");
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