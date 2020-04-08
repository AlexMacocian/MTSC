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

        public static Message GetMessage(SafeNetworkStream safeStream, SslStream sslStream)
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
            safeStream.PrepareProtectedRead();
            var buffer = new byte[256];
            var ms = new MemoryStream();
            var bytesRead = 0;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);
            safeStream.EndProtectedRead(out var _);
            return new Message((uint)ms.Length, ms.ToArray());
        }

        public static Message GetMessage(ClientData clientData)
        {
            Stream stream;
            if (clientData.SslStream != null)
            {
                stream = clientData.SslStream;
            }
            else
            {
                stream = clientData.SafeNetworkStream;
            }
            clientData.SafeNetworkStream.PrepareProtectedRead();
            var availableBytes = clientData.SafeNetworkStream.AvailableBytes;
            var buffer = new byte[availableBytes];
            var bytesRead = 0;
            while(bytesRead < availableBytes)
            {
                var bytesReadThisTurn = stream.Read(buffer, bytesRead, (int)availableBytes - bytesRead);
                if(bytesReadThisTurn == 0)
                {
                    break;
                }
                bytesRead += bytesReadThisTurn;
            }
            clientData.SafeNetworkStream.EndProtectedRead(out var _);
            Message message = new Message((uint)buffer.Length, buffer);
            return message;
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
