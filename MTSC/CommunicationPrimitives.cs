using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace MTSC
{
    static class CommunicationPrimitives
    {
        public static string RequestPublicKey = "REQPBKEY";
        public static string SendPublicKey = "PUBKEY";
        public static string SendEncryptionKey = "SYMKEY";
        public static string AcceptEncryptionKey = "SYMKEYOK";

        public static Message GetMessage(TcpClient client, SslStream sslStream = null)
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
            uint messageLength = (uint)client.Available;
            byte[] messageBuffer = new byte[messageLength];
            if (messageLength > 0)
            {
                stream.Read(messageBuffer, 0, (int)messageLength);
            }
            Message message = new Message(messageLength, messageBuffer);
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
            Message message = new Message((ushort)msgData.Length, msgData);
            return message;
        }
    }
}
