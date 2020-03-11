using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;

namespace MTSC.Common.WebSockets.RoutingModules
{
    public abstract class WebsocketRouteBase
    {
        public void CallConnectionInitialized(Server server, WebsocketRoutingHandler handler, ClientData client)
        {
            ConnectionInitialized(server, handler, client);
        }
        public void CallHandleReceivedMessage(Server server, WebsocketRoutingHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            HandleReceivedMessage(server, handler, client, receivedMessage);
        }
        public void CallConnectionClosed(Server server, WebsocketRoutingHandler handler, ClientData client)
        {
            ConnectionClosed(server, handler, client);
        }
        
        public void SendMessage(WebsocketMessage message, ClientData client, WebsocketRoutingHandler handler)
        {
            handler.QueueMessage(client, message);
        }

        public abstract void ConnectionInitialized(Server server, WebsocketRoutingHandler handler, ClientData client);
        public abstract void HandleReceivedMessage(Server server, WebsocketRoutingHandler handler, ClientData client, WebsocketMessage receivedMessage);
        public abstract void ConnectionClosed(Server server, WebsocketRoutingHandler handler, ClientData client);
    }
    public abstract class WebsocketRouteBase<TReceive> : WebsocketRouteBase
    {
        private Func<WebsocketMessage, TReceive> receiveTemplate;

        public WebsocketRouteBase(Func<WebsocketMessage, TReceive> receiveTemplate)
        {
            this.receiveTemplate = receiveTemplate;
        }

        public WebsocketRouteBase()
        {

        }

        public WebsocketRouteBase<TReceive> WithReceiveTemplateProvider(Func<WebsocketMessage, TReceive> templateProvider)
        {
            this.receiveTemplate = templateProvider;
            return this;
        }

        public override void HandleReceivedMessage(Server server, WebsocketRoutingHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            HandleReceivedMessage(server, handler, client, receiveTemplate.Invoke(receivedMessage));
        }

        public abstract void HandleReceivedMessage(Server server, WebsocketRoutingHandler handler, ClientData client, TReceive message);
    }
    public abstract class WebsocketRouteBase<TReceive, TSend> : WebsocketRouteBase
    {
        private Func<WebsocketMessage, TReceive> receiveTemplate;
        private Func<TSend, WebsocketMessage> sendTemplate;

        public WebsocketRouteBase(Func<WebsocketMessage, TReceive> receiveTemplate, Func<TSend, WebsocketMessage> sendTemplate)
        {
            this.receiveTemplate = receiveTemplate;
            this.sendTemplate = sendTemplate;
        }

        public WebsocketRouteBase()
        {

        }

        public WebsocketRouteBase<TReceive, TSend> WithReceiveTemplateProvider(Func<WebsocketMessage, TReceive> templateProvider)
        {
            this.receiveTemplate = templateProvider;
            return this;
        }

        public WebsocketRouteBase<TReceive, TSend> WithSendTemplateProvider(Func<TSend, WebsocketMessage> templateProvider)
        {
            this.sendTemplate = templateProvider;
            return this;
        }

        public void SendMessage(TSend message, ClientData client, WebsocketRoutingHandler handler)
        {
            base.SendMessage(sendTemplate.Invoke(message), client, handler);
        }

        public override void HandleReceivedMessage(Server server, WebsocketRoutingHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            HandleReceivedMessage(server, handler, client, receiveTemplate.Invoke(receivedMessage));
        }

        public abstract void HandleReceivedMessage(Server server, WebsocketRoutingHandler handler, ClientData client, TReceive message);
    }
}
