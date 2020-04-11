using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.Common
{
    public class TimeoutSuppressedStream : Stream
    {
        NetworkStream innerStream;

        public TimeoutSuppressedStream(TcpClient tcpClient)
        {
            innerStream = tcpClient.GetStream();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return innerStream.Read(buffer, offset, count);
            }
            catch (IOException lException)
            {
                SocketException lInnerException = lException.InnerException as SocketException;
                if (lInnerException != null && lInnerException.SocketErrorCode == SocketError.TimedOut)
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


        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanTimeout => innerStream.CanTimeout;
        public override bool CanWrite => innerStream.CanWrite;
        public virtual bool DataAvailable => innerStream.DataAvailable;
        public override long Length => innerStream.Length;
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state) => innerStream.BeginRead(buffer, offset, size, callback, state);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state) => innerStream.BeginWrite(buffer, offset, size, callback, state);
        public override int EndRead(IAsyncResult asyncResult) => innerStream.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) => innerStream.EndWrite(asyncResult);
        public override void Flush() => innerStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => innerStream.FlushAsync(cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);
        public override void SetLength(long value) => innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);

        public override long Position
        {
            get { return innerStream.Position; }
            set { innerStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return innerStream.ReadTimeout; }
            set { innerStream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return innerStream.WriteTimeout; }
            set { innerStream.WriteTimeout = value; }
        }
    }
}
