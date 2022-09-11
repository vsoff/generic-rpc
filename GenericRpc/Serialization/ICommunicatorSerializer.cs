using System;

namespace GenericRpc.Serialization
{
    public interface ICommunicatorSerializer
    {
        byte[] Serialize(object value, Type type);
        object Deserialize(byte[] bytes, Type type);
    }
}