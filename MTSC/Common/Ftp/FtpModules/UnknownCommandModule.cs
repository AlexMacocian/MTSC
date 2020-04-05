using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;

namespace MTSC.Common.Ftp.FtpModules
{
    public class UnknownCommandModule : IFtpModule
    {
        bool IFtpModule.HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server)
        {
            handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.CommandNotImplemented, Message = "Command not implemented" });
            return true;
        }
    }
}
