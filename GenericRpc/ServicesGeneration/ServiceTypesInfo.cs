using System;

namespace GenericRpc.ServicesGeneration
{
    internal class ServiceTypesInfo
    {
        public string ServiceName => InterfaceType.Name;
        public Type InterfaceType { get; }
        public Type RealizationType { get; }
        public bool IsProxy { get; }

        public ServiceTypesInfo(Type interfaceType, Type realizationType, bool isProxy)
        {
            InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));
            RealizationType = realizationType ?? throw new ArgumentNullException(nameof(realizationType));
            IsProxy = isProxy;
        }
    }
}