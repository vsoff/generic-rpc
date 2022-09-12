using GenericRpc.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GenericRpc.SocketTransport
{
    public static class SocketHelper
    {
        private const int DefaultBufferSize = 256;

        public static async Task<bool> TrySendPackageMessageAsync(this Socket socket, RpcMessage message)
        {
            if (socket == null) throw new ArgumentNullException(nameof(socket));

            try
            {
                if (!socket.Connected)
                    return false;

                await SendMessageAsync(socket, message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sends a <see cref="PackageMessage"/> on a <see cref="Socket"/>.
        /// </summary>
        public static async Task SendMessageAsync(this Socket socket, RpcMessage message)
        {
            if (socket == null) throw new ArgumentNullException(nameof(socket));
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Serializing package.
            var packageData = RpcMessageSerializer.Serialize(message);

            // Adding package length.
            var packageDataLength = BitConverter.GetBytes(packageData.Length);
            var fullResponse = new byte[packageData.Length + packageDataLength.Length];
            packageDataLength.CopyTo(fullResponse, 0);
            packageData.CopyTo(fullResponse, packageDataLength.Length);

            // Sending package.
            await socket.SendAsync(new ArraySegment<byte>(fullResponse), SocketFlags.None);
        }

        public static async IAsyncEnumerable<RpcMessage> StartReceiveMessagesAsync(this Socket socket)
        {
            byte[] unreadedBytes = new byte[0];
            while (socket.Connected)
            {
                using (var stream = new MemoryStream())
                {
                    //byte[] packageBytes;
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        // Revert unreaded bytes.
                        stream.Write(unreadedBytes, 0, unreadedBytes.Length);

                        // Read all data from socket and write to stream.
                        ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[DefaultBufferSize]);
                        do
                        {
                            var dataLength = await socket.ReceiveAsync(buffer, SocketFlags.None);

                            if (dataLength == DefaultBufferSize) writer.Write(buffer.ToArray());
                            else writer.Write(buffer.ToArray(), 0, dataLength);
                        } while (socket.Available > 0);

                        // Reset position for reading.
                        stream.Position = 0;

                        // Read all messages one by one.
                        while (true)
                        {
                            // Check is data fully readed.
                            var unreadedLength = stream.Length - stream.Position;
                            if (unreadedLength == 0)
                            {
                                break;
                            }

                            // Check is package fully readed.
                            var packageLength = reader.ReadInt32();
                            if (unreadedLength >= packageLength)
                            {
                                var packageMessage = RpcMessageSerializer.Read(reader);
                                yield return packageMessage;
                            }
                            else
                            {
                                // Revert package length position.
                                const int packageLengthBytes = 4;
                                stream.Position -= packageLengthBytes;
                                break;
                            }
                        }

                        // Save unreaded bytes.
                        unreadedBytes = ReadUnreadedBytes(stream);
                    }
                }
            }
        }

        private static byte[] ReadUnreadedBytes(Stream stream)
        {
            if (stream.Position > int.MaxValue)
                throw new GenericRpcSocketTransportException("Too many bytes unreaded");

            var unreadedBytes = new byte[stream.Length - stream.Position];
            if (unreadedBytes.Length > 0)
                stream.Read(unreadedBytes, 0, unreadedBytes.Length);
            return unreadedBytes;
        }
    }
}