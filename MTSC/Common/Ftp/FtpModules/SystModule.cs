using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Ftp.FtpModules
{
    public class SystModule : IFtpModule
    {
        bool IFtpModule.HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server)
        {
            if (request.Command == FtpRequestCommands.SYST)
            {
                handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.NAMESystemType, Message = "MTSC Webserver " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version });
                return true;
            }
            return false;
        }
    }
}
