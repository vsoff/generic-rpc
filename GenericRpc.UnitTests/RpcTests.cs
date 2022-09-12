using GenericRpc.Serialization;
using GenericRpc.SocketTransport;
using GenericRpc.UnitTests.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                .SetTransportLayer(new SocketTransportLayer())
                .RegisterListenerService<IExampleService>(new ServerExampleService())
                .Build();

            var clientCommunicator = new CommunicatorBuilder()
                .SetSerializer(new DefaultCommunicatorSerializer())
                .SetTransportLayer(new SocketTransportLayer())
                .RegisterSpeakerService<IExampleService>()
                .Build();

            var service = clientCommunicator.GetSpeakerService<IExampleService>();
            var sum = service.Sum(123, 321);
            Assert.AreEqual(444, sum);

            service.ShowMessage("Hello server!", "gg2");
        }
    }
}