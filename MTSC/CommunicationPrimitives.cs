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
            byte[] messageBuffer = new byte[4 + message.MessageLength];
            byte[] lengthBuffer = BitConverter.GetBytes(message.MessageLength);
            uint length = message.MessageLength;
            messageBuffer[0] = lengthBuffer[0];
            messageBuffer[1] = lengthBuffer[1];
            messageBuffer[2] = lengthBuffer[2];
            messageBuffer[3] = lengthBuffer[3];
            if (message.MessageLength > 0)
            {
                Array.Copy(message.MessageBytes, 0, messageBuffer, 4, length);
            }
            stream.Write(messageBuffer, 0, messageBuffer.Length);
        }

        public static Message BuildMessage(byte[] msgData)
        {
            Message message = new Message((ushort)msgData.Length, msgData);
            return message;
        }
    }
}
