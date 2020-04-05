using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MTSC.Common.Ftp.FtpModules
{
    public partial class FileSystemModule : IFtpModule
    {
        public string RootPath { get; set; } = Path.GetFullPath("/src/");

        public FileSystemModule WithRootPath(string rootPath)
        {
            this.RootPath = Path.GetFullPath(rootPath);
            return this;
        }

        bool IFtpModule.HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server)
        {
            if (!Directory.Exists(Path.GetFullPath(RootPath)))
            {
                server.Log($"Root path [{Path.GetFullPath(RootPath)}] doesn't exist! Creating it now!");
                Directory.CreateDirectory(Path.GetFullPath(RootPath));
            }

            if (!client.Resources.TryGetResource<FtpData>(out var ftpData))
            {
                client.Resources.SetResource(new FtpData { CurrentDirectory = Path.GetFullPath(RootPath) });
            }

            if (request.Command == FtpRequestCommands.PWD)
            {
                handler.QueueFtpResponse(server, client, PrintCurrentDirectory(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.CWD)
            {
                handler.QueueFtpResponse(server, client, ChangeCurrentDirectory(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.CDUP)
            {
                handler.QueueFtpResponse(server, client, ChangeToParentDirectory(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.RMD)
            {
                handler.QueueFtpResponse(server, client, RemoveDirectory(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.DELE)
            {
                handler.QueueFtpResponse(server, client, RemoveFile(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.MKD)
            {
                handler.QueueFtpResponse(server, client, CreateDirectory(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.RETR)
            {
                if (!ftpData.TransferDetails.ConnectionOpen)
                {
                    handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.FileStatusOkay, Message = "Opening data connection" });
                    ftpData.OpenDataConnection();
                }
                handler.QueueFtpResponse(server, client, RetrieveFile(request, ftpData));
                ftpData.CloseDataConnection();
                return true;
            }
            else if (request.Command == FtpRequestCommands.STOR)
            {
                if (!ftpData.TransferDetails.ConnectionOpen)
                {
                    handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.FileStatusOkay, Message = "Opening data connection" });
                    ftpData.OpenDataConnection();
                }
                handler.QueueFtpResponse(server, client, StoreFile(request, ftpData));
                ftpData.CloseDataConnection();
                return true;
            }
            else if (request.Command == FtpRequestCommands.TYPE)
            {
                handler.QueueFtpResponse(server, client, HandleTypeArguments(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.PASV)
            {
                handler.QueueFtpResponse(server, client, EnablePassiveMode(request, ftpData, client));
                return true;
            }
            else if (request.Command == FtpRequestCommands.PORT)
            {
                handler.QueueFtpResponse(server, client, EnableActiveMode(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.LIST) 
            {
                if (!ftpData.TransferDetails.ConnectionOpen)
                {
                    handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.FileStatusOkay, Message = "Opening data connection" });
                    ftpData.OpenDataConnection();
                }
                handler.QueueFtpResponse(server, client, ListFolder(request, ftpData));
                ftpData.CloseDataConnection();
                return true;
            }

            return false;
        }

        private FtpResponse StoreFile(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ? Path.GetFullPath(request.Arguments[0]) : null;

            if (path != null)
            {
                using (var fs = File.Create(path))
                {
                    while(ftpData.AvailableBytes() > 0) 
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
            var path = request.Arguments.Length > 0 ? Path.GetFullPath(request.Arguments[0]) : null;

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

        private FtpResponse CreateDirectory(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ? Path.GetFullPath(request.Arguments[0]) : null;

            if (path != null)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    return new FtpResponse { StatusCode = FtpResponseCodes.RequestedFileActionOkay, Message = $"Created directory {request.Arguments[0]}!" };
                }
                else
                {
                    return new FtpResponse { StatusCode = FtpResponseCodes.RequestedFileActionNotTaken, Message = $"Directory {request.Arguments[0]} already exists!" };
                }
            }
            else
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.RequestedActionAborted, Message = $"No path argument provided!" };
            }
        }

        private FtpResponse RemoveFile(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ? Path.GetFullPath(request.Arguments[0]) : null;

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

        private FtpResponse RemoveDirectory(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ? Path.GetFullPath(request.Arguments[0]) : null;

            if(path != null)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path);
                    return new FtpResponse { StatusCode = FtpResponseCodes.RequestedFileActionOkay, Message = $"Removed directory {request.Arguments[0]}!" };
                }
                else
                {
                    return new FtpResponse { StatusCode = FtpResponseCodes.FileUnavailable, Message = $"Directory {request.Arguments[0]} not found!" };
                }
            }
            else
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.RequestedActionAborted, Message = $"No path argument provided!" };
            }
        }

        private FtpResponse ChangeToParentDirectory(FtpRequest request, FtpData ftpData) 
        {
            var directoryInfo = Directory.GetParent(ftpData.CurrentDirectory);
            if (!directoryInfo.FullName.IsSubPathOf(RootPath))
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.SyntaxError, Message = "Cannot CDUP from root directory" };
            }
            else
            {
                ftpData.CurrentDirectory = directoryInfo.FullName;
                return new FtpResponse { StatusCode = FtpResponseCodes.ActionCompleted, Message = $"Changed working directory to {directoryInfo.Name}" };
            }
        }

        private FtpResponse ChangeCurrentDirectory(FtpRequest request, FtpData ftpData)
        {
            var directory = this.RootPath + request.Arguments[0];
            ftpData.CurrentDirectory = Path.GetFullPath(directory);
            return new FtpResponse { StatusCode = FtpResponseCodes.ActionCompleted, Message = $"Changed working directory to {request.Arguments[0]}" };
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
            byte[] address = ((IPEndPoint)client.TcpClient.Client.LocalEndPoint).Address.GetAddressBytes();
            byte[] portBytes = BitConverter.GetBytes((short)ftpData.TransferDetails.LocalDataPort);

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

        private FtpResponse ListFolder(FtpRequest request, FtpData ftpData)
        {
            var path = GetPathInfo(ftpData, request.Arguments.Length > 0 ? request.Arguments[0] : ftpData.CurrentDirectory);
            if (path != null)
            {
                IEnumerable<string> directories = Directory.EnumerateDirectories(path);
                IEnumerable<string> files = Directory.EnumerateFiles(path);

                foreach (string dir in directories)
                {
                    DateTime editDate = Directory.GetLastWriteTime(dir);

                    string date = editDate < DateTime.Now.Subtract(TimeSpan.FromDays(180)) ?
                        editDate.ToString("MMM dd  yyyy", CultureInfo.InvariantCulture) :
                        editDate.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture);

                    var data = "drwxr-xr-x    2 2003     2003         4096 " + date + ' ' + Path.GetFileName(dir);
                    var bytes = Encoding.UTF8.GetBytes(data);
                    ftpData.SendData(bytes, bytes.Length);
                }

                foreach (string file in files)
                {
                    FileInfo f = new FileInfo(file);

                    string date = f.LastWriteTime < DateTime.Now.Subtract(TimeSpan.FromDays(180)) ?
                        f.LastWriteTime.ToString("MMM dd  yyyy", CultureInfo.InvariantCulture) :
                        f.LastWriteTime.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture);

                    var line = "-rw-r--r--    2 2003     2003     ";

                    var length = f.Length.ToString(CultureInfo.InvariantCulture);

                    if (length.Length < 8)
                    {
                        for (int i = 0; i < 8 - length.Length; i++)
                        {
                            line += ' ';
                        }
                    }

                    line += length + ' ' + date + ' ' + f.Name;
                    var bytes = Encoding.UTF8.GetBytes(line);
                    ftpData.SendData(bytes, bytes.Length);
                }
            }
            return new FtpResponse { StatusCode = FtpResponseCodes.ClosingDataConnection, Message = "Transfer complete" };
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

        private FtpResponse PrintCurrentDirectory(FtpRequest request, FtpData ftpData)
        {
            var relativePath = ftpData.CurrentDirectory.RelativePath(this.RootPath);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                relativePath = "\\";
            }
            return new FtpResponse { StatusCode = FtpResponseCodes.PATHNAMECreated, Message = $"\"{relativePath}\" is current directory" };
        }

        private string GetPathInfo(FtpData ftpData, string path)
        {
            if (path == null)
            {
                path = string.Empty;
            }

            if (path == "/")
            {
                return this.RootPath;
            }
            else if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                path = new FileInfo(Path.Combine(this.RootPath, path.Substring(1))).FullName;
            }
            else
            {
                path = new FileInfo(Path.Combine(ftpData.CurrentDirectory, path)).FullName;
            }

            return path.IsSubPathOf(this.RootPath) ? path : null;
        }
    }
}
