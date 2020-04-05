using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Ftp.FtpModules
{
    public class QuitModule : IFtpModule
    {
        bool IFtpModule.HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server)
        {
            if (request.Command == FtpRequestCommands.QUIT)
            {
                handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.ServiceClosingControlConnection, Message = "Service closing control connection" });
                return true;
            }

            return false;
        }
    }
}
