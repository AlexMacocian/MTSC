using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common.WebSockets
{
    /// <summary>
    /// Class containing the bytes of a websocket received message.
    /// </summary>
    public class WebsocketMessage
    {
        public enum Opcodes
        {
            Data = 1,
            Text = 2,
            Close = 8,
            Ping = 9,
            Pong = 10
        }

        byte controlByte = new byte();
        byte[] lengthBytes;
        byte[] data;
        /// <summary>
        /// FIN bit.
        /// </summary>
        public bool FIN { get => (controlByte & 0x80) == 0x80; set => controlByte = (byte)(value? controlByte | 0x80 : controlByte & 0x7F);}
        /// <summary>
        /// Frame Opcode
        /// </summary>
        /// <remarks>Gets and sets the 4 lower bits of the first byte.</remarks>
        public Opcodes Opcode { get => (Opcodes)(controlByte & 0xF);
            set => controlByte = (byte)((controlByte & 0xF0) | ((int)value & 0xF)); }
        /// <summary>
        /// Mask bit.
        /// </summary>
        public bool Masked { get => (lengthBytes[0] & 0x80) == 0x1; set => lengthBytes[0] = (byte)(value ? lengthBytes[0] | 0x80 : lengthBytes[0] & 0x7F); }
        /// <summary>
        /// Length of message.
        /// </summary>
        public ulong MessageLength
        {
            get
            {
                if ((lengthBytes[0] & 0x7F) <= 125)
                {
                    return (ulong)(lengthBytes[0] & 0x7F);
                }
                else if ((lengthBytes[0] & 0x7F) == 126)
                {
                    return (ulong)((lengthBytes[2] << 8) + lengthBytes[1]);
                }
                else if ((lengthBytes[0] & 0x7F) == 127)
                {
                    return (ulong)((lengthBytes[8] << 56) + (lengthBytes[7] << 48) + (lengthBytes[6] << 40) + (lengthBytes[5] << 32) + 
                        (lengthBytes[4] << 24) + (lengthBytes[3] << 16) + (lengthBytes[2] << 8) + lengthBytes[1]);
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if(value <= 125)
                {
                    if(lengthBytes.Length != 1)
                    {
                        byte[] newLengthBytes = new byte[1];
                        newLengthBytes[0] = lengthBytes[0];
                        lengthBytes = newLengthBytes;
                    }
                    lengthBytes[0] = (byte)((lengthBytes[0] & 0x80) | ((byte)value & 0x7F));
                }
                else if(value <= UInt16.MaxValue)
                {
                    if (lengthBytes.Length != 3)
                    {
                        byte[] newLengthBytes = new byte[3];
                        newLengthBytes[0] = (byte)(lengthBytes[0] & 0x80);
                        newLengthBytes[0] += 126;
                        lengthBytes = newLengthBytes;
                    }
                    for(int i = 1; i < 3; i++)
                    {
                        lengthBytes[i] = (byte)(value & 0xFF);
                        value >>= 8;
                    }
                }
                else if(value <= UInt64.MaxValue)
                {
                    if (lengthBytes.Length != 8)
                    {
                        byte[] newLengthBytes = new byte[9];
                        newLengthBytes[0] = (byte)(lengthBytes[0] & 0x80);
                        newLengthBytes[0] += 127;
                        lengthBytes = newLengthBytes;
                    }
                    for (int i = 1; i < 9; i++)
                    {
                        lengthBytes[i] = (byte)(value & 0xFF);
                        value >>= 8;
                    }
                }
                else
                {

                }
            }
        }
        /// <summary>
        /// Mask of the message.
        /// </summary>
        public byte[] Mask { get; set; }
        /// <summary>
        /// Data of the message.
        /// </summary>
        public byte[] Data
        {
            get => data;
            set
            {
                MessageLength = (ulong)value.Length;
                data = value;
            }
        }

        /// <summary>
        /// Creates a new instance of websocket message containing the given message.
        /// </summary>
        /// <param name="messageBytes">Byte array containing the message bytes.</param>
        public WebsocketMessage(byte[] messageBytes)
        {
            controlByte = messageBytes[0];
            Mask = new byte[4];
            ulong dataLength = 0;
            int dataIndex = 0;
            if ((messageBytes[1] & 0x7F) <= 125)
            {
                lengthBytes = new byte[1];
                lengthBytes[0] = messageBytes[1];
                dataLength = (ulong)(messageBytes[1] & 0x7F);
                dataIndex = 2;
            }
            else if ((messageBytes[1] & 0x7F) == 126)
            {
                dataLength = (ulong)((messageBytes[3] << 8) + messageBytes[2]);
                lengthBytes = new byte[3];
                Array.Copy(messageBytes, 1, lengthBytes, 0, 3);
                dataIndex = 4;
            }
            else if ((messageBytes[1] & 0x7F) == 127)
            {
                dataLength = (ulong)((messageBytes[9] << 56) + (messageBytes[8] << 48) + (messageBytes[7] << 40) + (messageBytes[6] << 32) +
                    (messageBytes[5] << 24) + (messageBytes[4] << 16) + (messageBytes[3] << 8) + messageBytes[2]);
                dataIndex = 10;
                lengthBytes = new byte[9];
                Array.Copy(messageBytes, 1, lengthBytes, 0, 9);
            }
            else
            {
                lengthBytes = new byte[0];
                throw new InvalidWebsocketFormatException("Length formatting is wrong");
            }
            if ((messageBytes[1] & 0x80) > 0)
            {
                Array.Copy(messageBytes, dataIndex, Mask, 0, 4);
                dataIndex += 4;
            }
            data = new byte[messageBytes.Length - dataIndex];
            for(ulong i = 0; i < dataLength; i++)
            {
                data[i] = (byte)(messageBytes[(ulong)dataIndex + i] ^ Mask[i % 4]);
            }
        }
        /// <summary>
        /// Creates a new instance of websocket message.
        /// </summary>
        public WebsocketMessage()
        {
            controlByte = new byte();
            Mask = new byte[4];
            data = new byte[0];
            lengthBytes = new byte[1];
        }
        /// <summary>
        /// Get the packed message bytes.
        /// </summary>
        /// <returns>An array containing the message.</returns>
        public byte[] GetMessageBytes()
        {
            if(data == null)
            {
                throw new NoDataException("There is no data in the message");
            }
            byte[] messageBytes = new byte[1 + lengthBytes.Length + (Masked ? 4 : 0) + data.Length];
            messageBytes[0] = controlByte;
            Array.Copy(lengthBytes, 0, messageBytes, 1, lengthBytes.Length);
            if (Masked)
            {
                Array.Copy(Mask, 0, messageBytes, lengthBytes.Length, Mask.Length);
            }
            Array.Copy(data, 0, messageBytes, lengthBytes.Length + (Masked ? 4 : 0), data.Length);
            return messageBytes;
        }
    }
}
