using Newtonsoft.Json;
using System;
using System.Text;

namespace GenericRpc.Serialization
{
    /// <summary>
    /// Dummy implementation of data serialization.
    /// </summary>
    public sealed class DefaultCommunicatorSerializer : ICommunicatorSerializer
    {
        public object Deserialize(byte[] bytes, Type type)
        {
            if (bytes == null) return null;

            var json = Encoding.UTF8.GetString(bytes);
            var data = JsonConvert.DeserializeObject(json, type);
            return data;
        }

        public byte[] Serialize(object value, Type type)
        {
            if (value == null) return null;

            var json = JsonConvert.SerializeObject(value);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }
    }
}