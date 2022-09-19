using System;

namespace GenericRpc.ServicesGeneration
{
    internal sealed class ServiceTypesInfo
    {
        public string ServiceName => InterfaceType.Name;
        public Type InterfaceType { get; }
        public Type RealizationType { get; }
        public bool IsProxy { get; }
        public Func<object> ListenerFactoryMethod { get; }

        public ServiceTypesInfo(Type interfaceType, Type realizationType, bool isProxy, Func<object> listenerFactoryMethod)
        {
            InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));
            RealizationType = realizationType ?? throw new ArgumentNullException(nameof(realizationType));
            ListenerFactoryMethod = listenerFactoryMethod;
            IsProxy = isProxy;
        }
    }
}