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
            byte b = bytes[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { bytes[3], bytes[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > bytes.Length)
                throw new Exception("The buffer length is small than the data length");

            byte[] key = new byte[] { bytes[keyIndex], bytes[keyIndex + 1], bytes[keyIndex + 2], bytes[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key[count % 4]);
                count++;
            }
            return Encoding.ASCII.GetString(bytes, dataIndex, dataLength);
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
