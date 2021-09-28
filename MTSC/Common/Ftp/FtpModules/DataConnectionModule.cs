using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;
using System.Net;
using System.Net.Sockets;

namespace MTSC.Common.Ftp.FtpModules
{
    public class DataConnectionModule : IFtpModule
    {
        bool IFtpModule.HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server)
        {
            if (!client.Resources.TryGetResource<FtpData>(out var ftpData))
            {
                client.Resources.SetResource(new FtpData());
            }

            if (request.Command == FtpRequestCommands.PASV)
            {
                handler.QueueFtpResponse(server, client, this.EnablePassiveMode(request, ftpData, client));
                return true;
            }
            else if (request.Command == FtpRequestCommands.PORT)
            {
                handler.QueueFtpResponse(server, client, this.EnableActiveMode(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.TYPE)
            {
                handler.QueueFtpResponse(server, client, this.HandleTypeArguments(request, ftpData));
                return true;
            }

            return false;
        }

        private FtpResponse EnableActiveMode(FtpRequest request, FtpData ftpData)
        {
            ftpData.TransferDetails.Socket?.Dispose();

            var portArgs = request.Arguments[0].Split(',');

            ftpData.TransferDetails.Mode = TransferDetails.TransferMode.Active;
            byte[] addressBytes = { byte.Parse(portArgs[0]), byte.Parse(portArgs[1]), byte.Parse(portArgs[2]), byte.Parse(portArgs[3]) };
            byte[] portBytes = { byte.Parse(portArgs[4]), byte.Parse(portArgs[5]) };

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(portBytes);
            }

            var address = new IPAddress(addressBytes);
            var port = BitConverter.ToUInt16(portBytes, 0);

            ftpData.TransferDetails.DestinationDataAddress = address;
            ftpData.TransferDetails.DestinationDataPort = port;

            ftpData.TransferDetails.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            return new FtpResponse { StatusCode = FtpResponseCodes.ActionCompleted, Message = "PORT Command Accepted" };
        }

        private FtpResponse EnablePassiveMode(FtpRequest request, FtpData ftpData, ClientData client)
        {
            ftpData.TransferDetails.Socket?.Dispose();

            ftpData.TransferDetails.Mode = TransferDetails.TransferMode.Passive;
            ftpData.TransferDetails.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ftpData.TransferDetails.Socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            var address = ((IPEndPoint)client.TcpClient.Client.LocalEndPoint).Address.GetAddressBytes();
            var portBytes = BitConverter.GetBytes((short)ftpData.TransferDetails.LocalDataPort);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(portBytes);
            }

            ftpData.TransferDetails.Socket.Listen(10);

            return new FtpResponse
            {
                StatusCode = FtpResponseCodes.EnterPassiveMode,
                Message = $"Entering Passive mode ({address[0]},{address[1]},{address[2]},{address[3]},{portBytes[0]},{portBytes[1]})"
            };
        }

        private FtpResponse HandleTypeArguments(FtpRequest request, FtpData ftpData)
        {
            if (request.Arguments[0] == "I")
            {
                ftpData.TransferDetails.Type = TransferDetails.TransferType.BINARY;
                return new FtpResponse { StatusCode = FtpResponseCodes.ActionCompleted, Message = "Set transfer encoding to binary" };
            }
            else if (request.Arguments[0] == "A")
            {
                ftpData.TransferDetails.Type = TransferDetails.TransferType.ASCII;
                return new FtpResponse { StatusCode = FtpResponseCodes.ActionCompleted, Message = "Set transfer encoding to ASCII" };
            }
            else
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.CommandNotImplementedForParameter, Message = $"Command not implemented for parameter {request.Arguments[0]}" };
            }
        }
    }
}
