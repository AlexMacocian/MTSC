namespace MTSC
{
    public sealed class Message
    {
        public uint MessageLength { get; private set; }
        public byte[] MessageBytes { get; private set; }
        public Message(uint messageLength, byte[] messageBytes)
        {
            this.MessageLength = messageLength;
            this.MessageBytes = messageBytes;
        }
    }
}
