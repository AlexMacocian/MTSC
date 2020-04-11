using MTSC.Common;
using MTSC.ServerSide;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC
{
    static class CommunicationPrimitives
    {
        public static string RequestPublicKey = "REQPBKEY";
        public static string SendPublicKey = "PUBKEY";
        public static string SendEncryptionKey = "SYMKEY";
        public static string AcceptEncryptionKey = "SYMKEYOK";

        public static Message GetMessage(TimeoutSuppressedStream safeStream, SslStream sslStream)
        {
            Stream stream;
            if (sslStream!= null)
            {
                stream = sslStream;
            }
            else
            {
                stream = safeStream;
            }
            var buffer = new byte[1024];
            var ms = new MemoryStream();
            stream.ReadTimeout = 1;
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);
            return new Message((uint)ms.Length, ms.ToArray());
        }

        public static Message GetMessage(ClientData client)
        {
            Stream stream;
            if (client.SslStream != null)
            {
                stream = client.SslStream;
            }
            else
            {
                stream = client.SafeNetworkStream;
            }

            var buffer = new byte[1024];
            var ms = new MemoryStream();
            stream.ReadTimeout = 500;
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);
            return new Message((uint)ms.Length, ms.ToArray());
        }

        public static void SendMessage(TcpClient client, Message message, SslStream sslStream = null)
        {
            Stream stream;
            if (sslStream != null)
            {
                stream = sslStream;
            }
            else
            {
                stream = client.GetStream();
            }
            stream.Write(message.MessageBytes, 0, (int)message.MessageLength);
            stream.Flush();
        }

        public static Message BuildMessage(byte[] msgData)
        {
            Message message = new Message((uint)msgData.Length, msgData);
            return message;
        }
    }
}
