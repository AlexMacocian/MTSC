using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTSC
{
    public static class CommunicationPrimitives
    {
        public static Message GetMessage(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] controlBuffer = new byte[5];
            stream.Read(controlBuffer, 0, 4);
            uint messageLength = BitConverter.ToUInt32(controlBuffer, 0);
            byte[] messageBuffer = new byte[messageLength];
            if (messageLength > 0)
            {
                stream.Read(messageBuffer, 0, (int)messageLength);
            }
            Message message = new Message(messageLength, messageBuffer);
            return message;
        }

        public static void SendMessage(TcpClient client, Message message)
        {
            NetworkStream stream = client.GetStream();
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
