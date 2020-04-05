using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Ftp.FtpModules
{
    public interface IFtpModule
    {
        bool HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server);
    }
}
