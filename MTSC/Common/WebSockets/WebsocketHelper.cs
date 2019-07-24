using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MTSC.Common.WebSockets
{
    /// <summary>
    /// Class with websocket helper functions.
    /// </summary>
    public class WebsocketHelper
    {
        /// <summary>
        /// Decode text websocket message.
        /// </summary>
        /// <param name="bytes">Byte array containing the received message.</param>
        /// <returns>String containing the received message.</returns>
        public static string DecodeMessage(byte[] bytes)
        {
            if(bytes[0] != 129)
            {
                throw new InvalidFirstByteException();
            }
            int startIndex = 1;
            int length = 0;
            if(bytes[1] <= 125)
            {
                startIndex = 2;
                length = bytes[1];
            }
            else if(bytes[1] == 126)
            {
                startIndex = 4;
                length = bytes[2] << 8;
                length += bytes[3];
            }
            else if(bytes[1] == 127)
            {
                startIndex = 10;
                length = bytes[2] << 56;
                length += bytes[3] << 48;
                length += bytes[4] << 40;
                length += bytes[5] << 32;
                length += bytes[6] << 24;
                length += bytes[7] << 16;
                length += bytes[8] << 8;
                length += bytes[9];
            }
            byte[] message = new byte[length];
            Array.Copy(bytes, startIndex, message, 0, length);
            return Encoding.UTF8.GetString(message);
        }
        /// <summary>
        /// Encode text websocket message.
        /// </summary>
        /// <param name="message">Message to encode.</param>
        /// <returns>Byte array containing the encoded message.</returns>
        public static byte[] EncodeMessage(string message)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.UTF8.GetBytes(message);
            byte[] frame = new byte[10];

            int indexStartRawData = -1;
            int length = bytesRaw.Length;

            frame[0] = (byte)129;
            if (length <= 125)
            {
                frame[1] = (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (byte)127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }
            return response;
        }
    }
}
