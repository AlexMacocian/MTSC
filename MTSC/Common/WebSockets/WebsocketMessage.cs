using MTSC.Exceptions;
using System;

namespace MTSC.Common.WebSockets
{
    /// <summary>
    /// Class containing the bytes of a websocket received message.
    /// </summary>
    public sealed class WebsocketMessage
    {
        public enum Opcodes
        {
            Text = 1,
            Binary = 2,
            Close = 8,
            Ping = 9,
            Pong = 10
        }

        byte controlByte = new();
        byte[] lengthBytes;
        byte[] data;
        /// <summary>
        /// FIN bit.
        /// </summary>
        public bool FIN { get => (this.controlByte & 0x80) == 0x80; set => this.controlByte = (byte)(value? this.controlByte | 0x80 : this.controlByte & 0x7F);}
        /// <summary>
        /// Frame Opcode
        /// </summary>
        /// <remarks>Gets and sets the 4 lower bits of the first byte.</remarks>
        public Opcodes Opcode { get => (Opcodes)(this.controlByte & 0xF);
            set => this.controlByte = (byte)((this.controlByte & 0xF0) | ((int)value & 0xF)); }
        /// <summary>
        /// Mask bit.
        /// </summary>
        public bool Masked { get => (this.lengthBytes[0] & 0x80) == 0x80; set => this.lengthBytes[0] = (byte)(value ? this.lengthBytes[0] | 0x80 : this.lengthBytes[0] & 0x7F); }
        /// <summary>
        /// Length of message.
        /// </summary>
        public ulong MessageLength
        {
            get
            {
                if ((this.lengthBytes[0] & 0x7F) <= 125)
                {
                    return (ulong)(this.lengthBytes[0] & 0x7F);
                }
                else if ((this.lengthBytes[0] & 0x7F) == 126)
                {
                    return (ulong)((this.lengthBytes[1] << 8) + this.lengthBytes[2]);
                }
                else if ((this.lengthBytes[0] & 0x7F) == 127)
                {
                    return (ulong)((this.lengthBytes[1] << 56) + (this.lengthBytes[2] << 48) + (this.lengthBytes[3] << 40) + (this.lengthBytes[4] << 32) + 
                        (this.lengthBytes[5] << 24) + (this.lengthBytes[6] << 16) + (this.lengthBytes[7] << 8) + this.lengthBytes[8]);
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
                    if(this.lengthBytes.Length != 1)
                    {
                        var newLengthBytes = new byte[1];
                        newLengthBytes[0] = this.lengthBytes[0];
                        this.lengthBytes = newLengthBytes;
                    }

                    this.lengthBytes[0] = (byte)((this.lengthBytes[0] & 0x80) | ((byte)value & 0x7F));
                }
                else if(value <= UInt16.MaxValue)
                {
                    if (this.lengthBytes.Length != 3)
                    {
                        var newLengthBytes = new byte[3];
                        newLengthBytes[0] = (byte)(this.lengthBytes[0] & 0x80);
                        newLengthBytes[0] += 126;
                        this.lengthBytes = newLengthBytes;
                    }

                    for(var i = 1; i < 3; i++)
                    {
                        this.lengthBytes[3 - i] = (byte)(value & 0xFF);
                        value >>= 8;
                    }
                }
                else if(value <= UInt64.MaxValue)
                {
                    if (this.lengthBytes.Length != 8)
                    {
                        var newLengthBytes = new byte[9];
                        newLengthBytes[0] = (byte)(this.lengthBytes[0] & 0x80);
                        newLengthBytes[0] += 127;
                        this.lengthBytes = newLengthBytes;
                    }

                    for (var i = 1; i < 9; i++)
                    {
                        this.lengthBytes[9 - i] = (byte)(value & 0xFF);
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
            get => this.data;
            set
            {
                this.MessageLength = (ulong)value.Length;
                this.data = value;
            }
        }

        /// <summary>
        /// Creates a new instance of websocket message containing the given message.
        /// </summary>
        /// <param name="messageBytes">Byte array containing the message bytes.</param>
        public WebsocketMessage(byte[] messageBytes)
        {
            this.controlByte = messageBytes[0];
            this.Mask = new byte[4];
            ulong dataLength = 0;
            var dataIndex = 0;
            if ((messageBytes[1] & 0x7F) <= 125)
            {
                this.lengthBytes = new byte[1];
                this.lengthBytes[0] = messageBytes[1];
                dataLength = (ulong)(messageBytes[1] & 0x7F);
                dataIndex = 2;
            }
            else if ((messageBytes[1] & 0x7F) == 126)
            {
                dataLength = (ulong)((messageBytes[2] << 8) + messageBytes[3]);
                this.lengthBytes = new byte[3];
                Array.Copy(messageBytes, 1, this.lengthBytes, 0, 3);
                dataIndex = 4;
            }
            else if ((messageBytes[1] & 0x7F) == 127)
            {
                dataLength = (ulong)((messageBytes[3] << 56) + (messageBytes[3] << 48) + (messageBytes[4] << 40) + (messageBytes[5] << 32) +
                    (messageBytes[6] << 24) + (messageBytes[7] << 16) + (messageBytes[8] << 8) + messageBytes[9]);
                dataIndex = 10;
                this.lengthBytes = new byte[9];
                Array.Copy(messageBytes, 1, this.lengthBytes, 0, 9);
            }
            else
            {
                this.lengthBytes = new byte[0];
                throw new InvalidWebsocketFormatException("Length formatting is wrong");
            }

            if ((messageBytes[1] & 0x80) > 0)
            {
                Array.Copy(messageBytes, dataIndex, this.Mask, 0, 4);
                dataIndex += 4;
            }

            this.data = new byte[dataLength];
            for(ulong i = 0; i < dataLength; i++)
            {
                this.data[i] = (byte)(messageBytes[(ulong)dataIndex + i] ^ this.Mask[i % 4]);
            }
        }
        /// <summary>
        /// Creates a new instance of websocket message.
        /// </summary>
        public WebsocketMessage()
        {
            this.controlByte = new byte();
            this.Mask = new byte[4];
            this.data = new byte[0];
            this.lengthBytes = new byte[1];
        }
        /// <summary>
        /// Get the packed message bytes.
        /// </summary>
        /// <returns>An array containing the message.</returns>
        public byte[] GetMessageBytes()
        {
            if(this.data == null)
            {
                throw new NoDataException("There is no data in the message");
            }

            var messageBytes = new byte[1 + this.lengthBytes.Length + (this.Masked ? 4 : 0) + this.data.Length];
            messageBytes[0] = this.controlByte;
            Array.Copy(this.lengthBytes, 0, messageBytes, 1, this.lengthBytes.Length);
            if (this.Masked)
            {
                Array.Copy(this.Mask, 0, messageBytes, 1 + this.lengthBytes.Length, this.Mask.Length);
            }

            for(var i = 0; i < this.data.Length; i++)
            {
                messageBytes[1 + this.lengthBytes.Length + (this.Masked ? 4 : 0) + i] = (byte)(this.Masked ? this.data[i] ^ this.Mask[i % 4] : this.data[i]);
            }

            return messageBytes;
        }
    }
}
