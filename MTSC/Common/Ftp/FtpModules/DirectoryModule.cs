using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MTSC.Common.Ftp.FtpModules
{
    public class DirectoryModule : IFtpModule
    {
        public string RootPath { get; set; } = Path.GetFullPath("src/");

        public DirectoryModule WithRootPath(string rootPath)
        {
            this.RootPath = rootPath;
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

            if (string.IsNullOrEmpty(ftpData.CurrentDirectory))
            {
                ftpData.CurrentDirectory = Path.GetFullPath(this.RootPath);
            }

            if (request.Command == FtpRequestCommands.MKD)
            {
                handler.QueueFtpResponse(server, client, CreateDirectory(request, ftpData));
                return true;
            }
            else if (request.Command == FtpRequestCommands.PWD)
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

        private FtpResponse PrintCurrentDirectory(FtpRequest request, FtpData ftpData)
        {
            var relativePath = ftpData.CurrentDirectory.RelativePath(Path.GetFullPath(this.RootPath));
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                relativePath = "\\";
            }
            else
            {
                relativePath = relativePath.Substring(this.RootPath.Length) + "\\";
            }
            return new FtpResponse { StatusCode = FtpResponseCodes.PATHNAMECreated, Message = $"\"{relativePath}\" is current directory" };
        }

        private FtpResponse ListFolder(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ?
                Path.GetFullPath(ftpData.CurrentDirectory + request.Arguments.Aggregate((prev, next) => prev + " " + next)) :
                ftpData.CurrentDirectory;
            IEnumerable<string> directories = Directory.EnumerateDirectories(path);
            IEnumerable<string> files = Directory.EnumerateFiles(path);

            foreach (string dir in directories)
            {
                DateTime editDate = Directory.GetLastWriteTime(dir);

                string date = editDate < DateTime.Now.Subtract(TimeSpan.FromDays(180)) ?
                    editDate.ToString("MMM dd  yyyy", CultureInfo.InvariantCulture) :
                    editDate.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture);

                var data = "drwxr-xr-x    2 2003     2003         4096 " + date + ' ' + Path.GetFileName(dir) + "\r\n";
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

                line += length + ' ' + date + ' ' + f.Name + "\r\n";
                var bytes = Encoding.UTF8.GetBytes(line);
                ftpData.SendData(bytes, bytes.Length);
            }
            return new FtpResponse { StatusCode = FtpResponseCodes.ClosingDataConnection, Message = "Transfer complete" };
        }

        private FtpResponse CreateDirectory(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ?
                Path.GetFullPath(ftpData.CurrentDirectory + "\\" + request.Arguments.Aggregate((prev, next) => prev + " " + next)) :
                null;

            if (path != null)
            {
                if (!path.IsSubPathOf(Path.GetFullPath(this.RootPath)))
                {
                    return new FtpResponse { StatusCode = FtpResponseCodes.RequestedFileActionNotTaken, Message = "Path not found!" };
                }

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

        private FtpResponse RemoveDirectory(FtpRequest request, FtpData ftpData)
        {
            var path = request.Arguments.Length > 0 ?
                Path.GetFullPath(ftpData.CurrentDirectory + "\\" + request.Arguments.Aggregate((prev, next) => prev + " " + next)) :
                ftpData.CurrentDirectory;

            if (!path.IsSubPathOf(Path.GetFullPath(this.RootPath)))
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.FileUnavailable, Message = "Directory not found!" };
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return new FtpResponse { StatusCode = FtpResponseCodes.RequestedFileActionOkay, Message = "Removed directory!" };
            }
            else
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.FileUnavailable, Message = "Directory not found!" };
            }
        }

        private FtpResponse ChangeToParentDirectory(FtpRequest request, FtpData ftpData)
        {
            var directoryInfo = Directory.GetParent(ftpData.CurrentDirectory);
            if (!directoryInfo.FullName.IsSubPathOf(Path.GetFullPath(RootPath)))
            {
                return new FtpResponse { StatusCode = FtpResponseCodes.SyntaxError, Message = "Cannot CDUP from root directory!" };
            }
            else
            {
                ftpData.CurrentDirectory = directoryInfo.FullName;
                return new FtpResponse { StatusCode = FtpResponseCodes.ActionCompleted, Message = $"Changed working directory!" };
            }
        }

        private FtpResponse ChangeCurrentDirectory(FtpRequest request, FtpData ftpData)
        {
            var directory = this.RootPath + request.Arguments.Aggregate((prev, next) => prev + " " + next);
            ftpData.CurrentDirectory = Path.GetFullPath(directory);
            return new FtpResponse { StatusCode = FtpResponseCodes.ActionCompleted, Message = "Changed working directory!" };
        }
    }
}
