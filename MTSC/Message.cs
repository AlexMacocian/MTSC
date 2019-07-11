using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC
{
    public struct Message
    {
        private uint messageLength;
        private byte[] messageBytes;
        public uint MessageLength { get => messageLength; }
        public byte[] MessageBytes { get => messageBytes; }
        public Message(uint messageLength, byte[] messageBytes)
        {
            this.messageLength = messageLength;
            this.messageBytes = messageBytes;
        }
    }
}
