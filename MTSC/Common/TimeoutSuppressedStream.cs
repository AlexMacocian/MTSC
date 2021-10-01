using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.Common
{
    public class TimeoutSuppressedStream : Stream
    {
        private readonly NetworkStream innerStream;

        public TimeoutSuppressedStream(NetworkStream networkStream)
        {
            this.innerStream = networkStream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return this.innerStream.Read(buffer, offset, count);
            }
            catch (IOException lException)
            {
                if (lException.InnerException is SocketException lInnerException && lInnerException.SocketErrorCode == SocketError.TimedOut)
                {
                    // Normally, a simple TimeOut on the read will cause SslStream to flip its lid
                    // However, if we suppress the IOException and just return 0 bytes read, this is ok.
                    // Note that this is not a "Socket.Read() returning 0 means the socket closed",
                    // this is a "Stream.Read() returning 0 means that no data is available"
                    return 0;
                }

                throw;
            }
        }
        public override bool CanRead => this.innerStream.CanRead;
        public override bool CanSeek => this.innerStream.CanSeek;
        public override bool CanTimeout => this.innerStream.CanTimeout;
        public override bool CanWrite => this.innerStream.CanWrite;
        public virtual bool DataAvailable => this.innerStream.DataAvailable;
        public override long Length => this.innerStream.Length;
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state) => this.innerStream.BeginRead(buffer, offset, size, callback, state);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state) => this.innerStream.BeginWrite(buffer, offset, size, callback, state);
        public override int EndRead(IAsyncResult asyncResult) => this.innerStream.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) => this.innerStream.EndWrite(asyncResult);
        public override void Flush() => this.innerStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => this.innerStream.FlushAsync(cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => this.innerStream.Seek(offset, origin);
        public override void SetLength(long value) => this.innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => this.innerStream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.innerStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override long Position
        {
            get { return this.innerStream.Position; }
            set { this.innerStream.Position = value; }
        }
        public override int ReadTimeout
        {
            get { return this.innerStream.ReadTimeout; }
            set { this.innerStream.ReadTimeout = value; }
        }
        public override int WriteTimeout
        {
            get { return this.innerStream.WriteTimeout; }
            set { this.innerStream.WriteTimeout = value; }
        }
    }
}
