using GenericRpc.Serialization;
using System;
using System.Diagnostics;

namespace GenericRpc.Transport
{
    public class SpeakerService
    {
        public static readonly string ExecuteMethodName = nameof(Execute);

        private readonly ITransportLayer _transportLayer;
        private readonly ICommunicatorSerializer _serializer;

        public SpeakerService(ITransportLayer transportLayer, ICommunicatorSerializer serializer)
        {
            _transportLayer = transportLayer ?? throw new ArgumentNullException(nameof(transportLayer));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        protected object Execute(string serviceName, string methodName, params object[] arguments)
        {
            Debug.WriteLine($"{serviceName} {methodName} {arguments.Length}");
            //var data = _serializer.Serialize(message, typeof(string));
            //_transportLayer.SendMessageAsync(new RpcMessage(nameof(IExampleService), nameof(ShowMessage), Guid.NewGuid(), RpcMessageType.Request, data));
            return default;
        }
    }
}
