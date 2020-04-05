using MTSC.Common.Ftp;
using MTSC.Common.Ftp.FtpModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Handlers
{
    /// <summary>
    /// Handler for FTP communication
    /// </summary>
    #warning Due to the way FTP works, this handler will wait a configurable amount of time before issuing that the server is ready. If the client has any communication in that period, this handler will cancel the ftp handling
    public class FtpHandler : IHandler
    {
        private readonly string welcomeMessage = "220 Server is ready\r\n";
        private readonly string commandUnknownMessage = "502 I DON'T KNOW\r\n";

        private enum State
        {
            Default,
            Initialized
        }

        private List<IFtpModule> ftpModules = new List<IFtpModule>();

        public TimeSpan ReadyDelay { get; set; } = TimeSpan.FromSeconds(1);

        public FtpHandler WithReadyDelay(TimeSpan delay)
        {
            this.ReadyDelay = delay;
            return this;
        }

        public FtpHandler AddModule(IFtpModule module)
        {
            ftpModules.Add(module);
            return this;
        }
        
        public void QueueFtpResponse(Server server, ClientData client, FtpResponse response)
        {
            server.QueueMessage(client, FtpResponse.ToBytes(response));
        }

        void IHandler.ClientRemoved(Server server, ClientData client)
        {
            
        }

        bool IHandler.HandleClient(Server server, ClientData client)
        {
            client.Resources.SetResource(State.Default);
            var issueTime = DateTime.Now;
            Task.Delay(ReadyDelay).ContinueWith((previousTask) =>
            {
                if(client.LastActivityTime < issueTime)
                {
                    client.Resources.SetResource(State.Initialized);
                    server.QueueMessage(client, Encoding.ASCII.GetBytes(welcomeMessage));
                }
            });
            return false;
        }

        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            if(client.Resources.TryGetResource<State>(out var state) && state == State.Initialized)
            {
                var request = FtpRequest.FromBytes(message.MessageBytes);
                var handled = false;
                foreach(IFtpModule module in ftpModules)
                {
                    if(module.HandleRequest(request, client, this, server))
                    {
                        handled = true;
                        break;
                    }
                }
                return handled;
            }
            return false;
        }

        bool IHandler.HandleSendMessage(Server server, ClientData client, ref Message message) => false;

        bool IHandler.PreHandleReceivedMessage(Server server, ClientData client, ref Message message) => false;

        void IHandler.Tick(Server server) { }
    }
}
