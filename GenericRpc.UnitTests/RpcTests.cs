using GenericRpc.Serialization;
using GenericRpc.Socket;
using GenericRpc.UnitTests.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenericRpc.UnitTests
{
    [TestClass]
    public class RpcTests
    {
        private const string _serverIp = "127.0.0.1";
        private const ushort _serverPort = 21337;

        [TestMethod]
        public void ComunicatorBuildTest()
        {
            var serverCommunicator = new CommunicatorBuilder()
                .SetSerializer(new DefaultCommunicatorSerializer())
                .SetTransportLayer(new SocketTransportLayer())
                .RegisterListenerService<IExampleService>(new ServerExampleService())
                .Build();
            serverCommunicator.Start(_serverIp, _serverPort);

            var clientCommunicator = new CommunicatorBuilder()
                .SetSerializer(new DefaultCommunicatorSerializer())
                .SetTransportLayer(new SocketTransportLayer())
                .RegisterSpeakerService<IExampleService>()
                .Build();
            clientCommunicator.Connect(_serverIp, _serverPort);

            var service = clientCommunicator.GetSpeakerService<IExampleService>();
            var sum = service.Sum(123, 321);
            Assert.AreEqual(444, sum);

            service.ShowMessage("Hello server!", "gg2");
        }
    }
}