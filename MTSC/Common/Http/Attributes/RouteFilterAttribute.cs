using MTSC.ServerSide;
using System;

namespace MTSC.Common.Http.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class RouteFilterAttribute : Attribute
    {
        public virtual RouteEnablerResponse HandleRequest(Server server, ClientData clientData, HttpRequest httpRequest) => RouteEnablerResponse.Accept;

        public virtual void HandleResponse(Server server, ClientData clientData, HttpResponse httpResponse)
        {
        }
    }
}
