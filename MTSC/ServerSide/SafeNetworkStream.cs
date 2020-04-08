using System.IO;
using System.Net.Sockets;

namespace MTSC.ServerSide
{
    public class SafeNetworkStream : Stream
    {
        private NetworkStream underlyingStream;
        private TcpClient tcpClient;
        private MemoryStream innerBuffer;

        public SafeNetworkStream(TcpClient tcpClient)
        {
            this.underlyingStream = tcpClient.GetStream();
            this.tcpClient = tcpClient;
            this.innerBuffer = new MemoryStream();
        }

        /// <summary>
        /// Number of bytes read from the stream
        /// </summary>
        public int BytesRead { get; private set; } = 0;

        public long AvailableBytes { get => this.innerBuffer != null ? innerBuffer.Length - innerBuffer.Position : 0; }

        public bool Protected { get; private set; } = false;

        public override bool CanRead => this.underlyingStream.CanRead;

        public override bool CanSeek => this.underlyingStream.CanSeek;

        public override bool CanWrite => this.underlyingStream.CanWrite;

        public override long Length => this.underlyingStream.Length;

        public override long Position { get => this.underlyingStream.Position; set => this.underlyingStream.Position = value; }

        public override void Flush() => this.underlyingStream.Flush();

        public void PrepareProtectedRead()
        {
            this.innerBuffer = new MemoryStream();
            var tempBuffer = new byte[tcpClient.Available];
            var tempBytesRead = this.underlyingStream.Read(tempBuffer, 0, tcpClient.Available);
            this.innerBuffer.Write(tempBuffer, 0, tempBytesRead);
            this.innerBuffer.Position = 0;
            BytesRead = 0;
            Protected = true;
        }

        public void EndProtectedRead(out byte[] remainingBytes)
        {
            remainingBytes = new byte[0];
            if(innerBuffer.Position < innerBuffer.Length)
            {
                remainingBytes = new byte[innerBuffer.Length - innerBuffer.Position];
                innerBuffer.Read(remainingBytes, 0, remainingBytes.Length);
            }
            this.innerBuffer.Dispose();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Protected)
            {
                var readBytes = this.innerBuffer.Read(buffer, offset, count);
                BytesRead += readBytes;
                return readBytes;
            }
            else
            {
                var readBytes = this.underlyingStream.Read(buffer, offset, count);
                BytesRead += readBytes;
                return readBytes;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => this.underlyingStream.Seek(offset, origin);

        public override void SetLength(long value) => this.underlyingStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => this.underlyingStream.Write(buffer, offset, count);
    }
}
