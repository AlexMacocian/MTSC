using System.Net;
using System.Net.Sockets;

namespace MTSC.Common.Ftp
{
    public class TransferDetails
    {
        public enum TransferType
        {
            ASCII,
            BINARY
        }
        public enum TransferMode
        {
            Active,
            Passive
        }

        public TransferType Type { get; set; } = TransferType.BINARY;
        public TransferMode Mode { get; set; } = TransferMode.Passive;
        public Socket Socket { get; set; }
        public IPAddress DestinationDataAddress { get; set; }
        public ushort DestinationDataPort { get; set; }
        public IPAddress LocalDataAddress { get => ((IPEndPoint)this.Socket.LocalEndPoint).Address; }
        public ushort LocalDataPort { get => (ushort)((IPEndPoint)this.Socket.LocalEndPoint).Port; }
        public bool ConnectionOpen { get; set; } = false;
    }
}
