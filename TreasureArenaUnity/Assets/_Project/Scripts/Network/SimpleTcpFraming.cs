using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TreasureArenaMR.Network
{
    public static class SimpleTcpFraming
    {
        public const int MaxFrameBytes = 256 * 1024;

        public static void WriteFrame(NetworkStream stream, string json)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (json == null)
                json = string.Empty;

            byte[] payload = Encoding.UTF8.GetBytes(json);
            if (payload.Length > MaxFrameBytes)
                throw new InvalidOperationException("frame_too_large");

            int networkLength = IPAddress.HostToNetworkOrder(payload.Length);
            byte[] lengthBytes = BitConverter.GetBytes(networkLength);
            stream.Write(lengthBytes, 0, lengthBytes.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        public static string ReadFrame(NetworkStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] lengthBytes = ReadExact(stream, 4);
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));
            if (length <= 0 || length > MaxFrameBytes)
                throw new InvalidDataException("invalid_frame_length: " + length);

            byte[] payload = ReadExact(stream, length);
            return Encoding.UTF8.GetString(payload);
        }

        private static byte[] ReadExact(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    throw new EndOfStreamException();

                offset += read;
            }

            return buffer;
        }
    }
}
