using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MTSC.Common.WebSockets
{
    /// <summary>
    /// Class with websocket helper functions.
    /// </summary>
    public class WebsocketHelper
    {
        private static RNGCryptoServiceProvider rngCryptoServiceProvider = new RNGCryptoServiceProvider();
        /// <summary>
        /// Decode text websocket message.
        /// </summary>
        /// <param name="bytes">Byte array containing the received message.</param>
        /// <returns>String containing the received message.</returns>
        public static string DecodeTextMessage(byte[] bytes)
        {
            byte b = bytes[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;
            bool masked = false;
            if((b & 128) > 127)
            {
                /*
                 * Message is masked.
                 */
                b -= (byte)128;
                masked = true;
            }
            if (b <= 125)
            {
                dataLength = b;
                keyIndex = 2;
                totalLength = dataLength + 2 + (masked == true ? 4 : 0);
            }
            else if (b == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { bytes[3], bytes[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 4 + (masked == true ? 4 : 0);
            }
            else if (b == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 10 +(masked == true ? 4 : 0);
            }

            if (totalLength > bytes.Length)
                throw new Exception("The buffer length is smaller than the data length");

            int dataIndex = keyIndex;
            if (masked)
            {
                dataIndex += 4;
                for (int i = dataIndex, count = 0; i < totalLength; i++, count++)
                {
                    bytes[i] = (byte)(bytes[i] ^ bytes[keyIndex + (count % 4)]);
                }
            }

            return Encoding.UTF8.GetString(bytes, dataIndex, dataLength);
        }
        /// <summary>
        /// Encode text websocket message.
        /// </summary>
        /// <param name="message">Message to encode.</param>
        /// <returns>Byte array containing the encoded message.</returns>
        public static byte[] EncodeTextMessage(string message, bool masked = false)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.UTF8.GetBytes(message);
            byte[] frame = new byte[10];

            int indexStartRawData = -1;
            int length = bytesRaw.Length;
            int maskLength = 0;
            if (masked)
            {
                maskLength = 4;
            }

            frame[0] = (byte)129;
            if (masked)
            {
                frame[1] = (byte)128;
            }
            if (length <= 125)
            {
                frame[1] += (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] += (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] += (byte)127;
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

            response = new byte[indexStartRawData + maskLength + length];

            int responseIdx = 0;

            //Add the frame bytes to the reponse
            for (int i = 0; i < indexStartRawData; i++)
            {
                response[responseIdx] = frame[i];
                responseIdx++;
            }

            rngCryptoServiceProvider.GetBytes(response, responseIdx, maskLength);

            responseIdx += maskLength;


            //Add the data bytes to the response
            for (int i = 0; i < length; i++)
            {
                response[responseIdx] = (byte)(bytesRaw[i] ^ (masked == true ? response[indexStartRawData + (i % 4)] : 0));
                responseIdx++;
            }
            return response;
        }
    }
}
