using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Http.Telemetry
{
    public interface IHttpLogger
    {
        void LogRequest(Server server, IHandler handler, ClientData client, HttpRequest request);
        void LogResponse(Server server, IHandler handler, ClientData client, HttpResponse response);
    }
}
