using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.Transport;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenericRpc
{
    public sealed class CommunicatorBuilder
    {
        private ICommunicatorSerializer _serializer;
        private ITransportLayer _transportLayer;
        private readonly HashSet<Type> _speakerServices = new HashSet<Type>();
        private readonly Dictionary<Type, object> _listenerServicebyInterface = new Dictionary<Type, object>();

        public CommunicatorBuilder SetSerializer(ICommunicatorSerializer serializer)
        {
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            _serializer = serializer;
            return this;
        }
        public CommunicatorBuilder SetTransportLayer(ITransportLayer transportLayer)
        {
            if (transportLayer == null) throw new ArgumentNullException(nameof(transportLayer));

            _transportLayer = transportLayer;
            return this;
        }
        public CommunicatorBuilder RegisterSpeakerService<TServiceInterface>()
        {
            if (!typeof(TServiceInterface).IsInterface)
                throw new GenericRpcException($"Type `{nameof(TServiceInterface)}` isn't an interface");

            if (_speakerServices.Contains(typeof(TServiceInterface)))
                throw new GenericRpcException($"Type `{nameof(TServiceInterface)}` already registered as speaker service");

            _speakerServices.Add(typeof(TServiceInterface));
            return this;
        }
        public CommunicatorBuilder RegisterListenerService<TServiceInterface>(TServiceInterface implementation)
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));

            if (!typeof(TServiceInterface).IsInterface)
                throw new GenericRpcException($"Type `{nameof(TServiceInterface)}` isn't an interface");

            if (_listenerServicebyInterface.ContainsKey(typeof(TServiceInterface)))
                throw new GenericRpcException($"Type `{nameof(TServiceInterface)}` already registered as listener service");

            _listenerServicebyInterface.Add(typeof(TServiceInterface), implementation);
            return this;
        }

        public Communicator Build()
        {
            if (_serializer == null) throw new GenericRpcException("Serializer shoudn't be null");
            if (_transportLayer == null) throw new GenericRpcException("Transport layer shoudn't be null");

            var speakerServiceByInterface = _speakerServices.ToDictionary(type => type, type => ClassGenerator.GenerateSpeakerInstance(type, _transportLayer, _serializer));

            return new Communicator(_serializer, _transportLayer, speakerServiceByInterface, _listenerServicebyInterface);
        }
    }
}
