using MTSC.Server;
using System;

namespace MTSC.Common.Http.RoutingModules
{
    public abstract class HttpRouteBase
    {
        public HttpResponse CallHandleRequest(HttpRequest request, ClientData client, Server.Server server)
        {
            return this.HandleRequest(request, client, server);
        }

        public abstract HttpResponse HandleRequest(HttpRequest request, ClientData client, Server.Server server);
    }
    public abstract class HttpRouteBase<T> : HttpRouteBase
    {
        private Func<HttpRequest, T> template;

        public HttpRouteBase(Func<HttpRequest, T> template)
        {
            this.template = template;
        }

        public HttpRouteBase()
        {

        }

        public HttpRouteBase WithTemplateProvider(Func<HttpRequest, T> templateProvider)
        {
            this.template = templateProvider;
            return this;
        }

        public override HttpResponse HandleRequest(HttpRequest request, ClientData client, Server.Server server)
        {
            return HandleRequest(template.Invoke(request), client, server);
        }

        public abstract HttpResponse HandleRequest(T request, ClientData client, Server.Server server);
    }
}
