using MTSC.ServerSide;
using System;

namespace MTSC.Common.Http.RoutingModules
{
    public abstract class HttpRouteBase
    {
        public HttpResponse CallHandleRequest(HttpRequest request, ClientData client, ServerSide.Server server)
        {
            return this.HandleRequest(request, client, server);
        }

        public abstract HttpResponse HandleRequest(HttpRequest request, ClientData client, ServerSide.Server server);
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

        public HttpRouteBase<T> WithTemplateProvider(Func<HttpRequest, T> templateProvider)
        {
            this.template = templateProvider;
            return this;
        }

        public override HttpResponse HandleRequest(HttpRequest request, ClientData client, ServerSide.Server server)
        {
            return HandleRequest(template.Invoke(request), client, server);
        }

        public abstract HttpResponse HandleRequest(T request, ClientData client, ServerSide.Server server);
    }
    public abstract class HttpRouteBase<TReceive, TSend> : HttpRouteBase
    {
        private Func<HttpRequest, TReceive> receiveTemplate;
        private Func<TSend, HttpResponse> sendTemplate;

        public HttpRouteBase(Func<HttpRequest, TReceive> receiveTemplate, Func<TSend, HttpResponse> sendTemplate)
        {
            this.receiveTemplate = receiveTemplate;
            this.sendTemplate = sendTemplate;
        }

        public HttpRouteBase()
        {

        }

        public HttpRouteBase<TReceive, TSend> WithReceiveTemplateProvider(Func<HttpRequest, TReceive> templateProvider)
        {
            this.receiveTemplate = templateProvider;
            return this;
        }

        public HttpRouteBase<TReceive, TSend> WithSendTemplateProvider(Func<TSend, HttpResponse> templateProvider)
        {
            this.sendTemplate = templateProvider;
            return this;
        }

        public override HttpResponse HandleRequest(HttpRequest request, ClientData client, ServerSide.Server server)
        {
            return sendTemplate.Invoke(HandleRequest(receiveTemplate.Invoke(request), client, server));
        }

        public abstract TSend HandleRequest(TReceive request, ClientData client, ServerSide.Server server);
    }
}
