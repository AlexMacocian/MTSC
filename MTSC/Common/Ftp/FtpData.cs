using System.IO;
using System.Net;

namespace MTSC.Common.Ftp
{
    public class FtpData
    {
        public string CurrentDirectory { get; set; }
        public TransferDetails TransferDetails { get; set; } = new TransferDetails();

        public void OpenDataConnection()
        {
            if(this.TransferDetails.Mode == TransferDetails.TransferMode.Active)
            {
                this.TransferDetails.Socket.Connect(new IPEndPoint(this.TransferDetails.DestinationDataAddress, this.TransferDetails.DestinationDataPort));
            }
            else
            {
                var dataConnection = this.TransferDetails.Socket.Accept();
                this.TransferDetails.Socket.Close();
                this.TransferDetails.Socket.Dispose();
                this.TransferDetails.Socket = dataConnection;
            }
            this.TransferDetails.ConnectionOpen = true;
        }

        public void CloseDataConnection()
        {
            this.TransferDetails.Socket.Close();
            this.TransferDetails.Socket.Dispose();
            this.TransferDetails.ConnectionOpen = false;
        }

        public void SendData(byte[] data, int length)
        {
            this.TransferDetails.Socket.Send(data, length, System.Net.Sockets.SocketFlags.None);
        }

        public int AvailableBytes()
        {
            return this.TransferDetails.Socket.Available;
        }

        public byte[] GetBytes(int length)
        {
            var buffer = new byte[length];
            this.TransferDetails.Socket.Receive(buffer, length, System.Net.Sockets.SocketFlags.None);
            return buffer;
        }
    }
}
