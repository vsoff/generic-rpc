using GenericRpc.Exceptions;
using GenericRpc.Serialization;
using GenericRpc.Transport;
using System;
using System.Collections.Generic;

namespace GenericRpc
{
    public sealed class Communicator
    {
        private readonly ICommunicatorSerializer _serializer;
        private readonly ITransportLayer _transportLayer;
        private readonly IReadOnlyDictionary<Type, object> _speakerServiceByInterface;
        private readonly IReadOnlyDictionary<Type, object> _listenerServiceByInterface;

        internal Communicator(
            ICommunicatorSerializer serializer,
            ITransportLayer transportLayer,
            IReadOnlyDictionary<Type, object> speakerServiceByInterface,
            IReadOnlyDictionary<Type, object> listenerServiceByInterface)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _transportLayer = transportLayer ?? throw new ArgumentNullException(nameof(transportLayer));
            _speakerServiceByInterface = speakerServiceByInterface ?? throw new ArgumentNullException(nameof(speakerServiceByInterface));
            _listenerServiceByInterface = listenerServiceByInterface ?? throw new ArgumentNullException(nameof(listenerServiceByInterface));
        }

        public void Start(string ip, ushort port)
        {
            if (_transportLayer.IsConnected)
                throw new GenericRpcException("Service already connected");

            _transportLayer.Start(ip, port);
        }
        public void Connect(string ip, ushort port)
        {
            if (_transportLayer.IsConnected)
                throw new GenericRpcException("Service already connected");

            _transportLayer.Connect(ip, port);
        }

        public TServiceInterface GetSpeakerService<TServiceInterface>()
        {
            if (!typeof(TServiceInterface).IsInterface)
                throw new GenericRpcException($"Type `{nameof(TServiceInterface)}` isn't an interface");

           if (! _speakerServiceByInterface.TryGetValue(typeof(TServiceInterface), out var service))
                throw new GenericRpcException($"Service with interface `{nameof(TServiceInterface)}` isn't found");

            return (TServiceInterface)service;
        }
    }
}