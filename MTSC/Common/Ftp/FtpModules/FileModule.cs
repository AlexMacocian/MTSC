using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;
using System.IO;
using System.Linq;

namespace MTSC.Common.Ftp.FtpModules
{
    public class FileModule : IFtpModule
    {
        bool IFtpModule.HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server)
        {
            if (!client.Resources.TryGetResource<FtpData>(out var ftpData))
            {
                client.Resources.SetResource(new FtpData());
            }

            if (request.Command == FtpRequestCommands.STOR)
            {
                if (!ftpData.TransferDetails.ConnectionOpen)
                {
                    handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.FileStatusOkay, Message = "Opening data connection" });
                    ftpData.OpenDataConnection();
                }

                handler.QueueFtpResponse(server, client, this.StoreFile(request, ftpData));
                ftpData.CloseDataConnection();
                return true;
            }
            else if (request.Command == FtpRequestCommands.DELE)
            {
                handler.QueueFtpResponse(server, client, this.RemoveFile(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.RETR)
            {
                if (!ftpData.TransferDetails.ConnectionOpen)
                {
                    handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.FileStatusOkay, Message = "Opening data connection" });
                    ftpData.OpenDataConnection();
                }

                handler.QueueFtpResponse(server, client, this.RetrieveFile(request, ftpData));
                ftpData.CloseDataConnection();
                return true;
            }

            return false;
        }

        private FtpResponse StoreFile(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ?
                Path.GetFullPath(ftpData.CurrentDirectory + "\\" + request.Arguments.Aggregate((prev, next) => prev + " " + next)) :
                null;

            if (path != null)
            {
                using (var fs = File.Create(path))
                {
                    while (ftpData.AvailableBytes() > 0)
                    {
                        var bytes = ftpData.GetBytes(ftpData.AvailableBytes());
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }

                return new FtpResponse { StatusCode = FtpResponseCodes.ClosingDataConnection, Message = "Transfer complete" };
            }
            else
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.RequestedActionAborted, Message = $"No path argument provided!" };
            }
        }

        private FtpResponse RetrieveFile(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ?
                Path.GetFullPath(ftpData.CurrentDirectory + request.Arguments.Aggregate((prev, next) => prev + " " + next)) :
                null;

            if (path != null)
            {
                using (var fs = File.OpenRead(path))
                {
                    var buffer = new byte[256];
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ftpData.SendData(buffer, bytesRead);
                    }
                }
            }

            return new FtpResponse { StatusCode = FtpResponseCodes.ClosingDataConnection, Message = "Transfer complete" };
        }

        private FtpResponse RemoveFile(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ?
                Path.GetFullPath(ftpData.CurrentDirectory + "\\" + request.Arguments.Aggregate((prev, next) => prev + " " + next)) :
                null;

            if (path != null)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return new FtpResponse { StatusCode = FtpResponseCodes.RequestedFileActionOkay, Message = $"Removed file {request.Arguments[0]}!" };
                }
                else
                {
                    return new FtpResponse { StatusCode = FtpResponseCodes.FileUnavailable, Message = $"File {request.Arguments[0]} not found!" };
                }
            }
            else
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.RequestedActionAborted, Message = $"No path argument provided!" };
            }
        }
    }
}
