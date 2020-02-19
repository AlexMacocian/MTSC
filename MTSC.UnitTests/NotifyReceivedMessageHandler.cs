using MTSC.Client.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.UnitTests
{
    public class NotifyReceivedMessageHandler : IHandler
    {
        public event EventHandler<Message> ReceivedMessage;

        void IHandler.Disconnected(Client.Client client) { }

        bool IHandler.HandleReceivedMessage(Client.Client client, Message message)
        {
            ReceivedMessage?.Invoke(this, message);
            return false;
        }

        bool IHandler.HandleSendMessage(Client.Client client, ref Message message) => false;

        bool IHandler.InitializeConnection(Client.Client client) => true;

        bool IHandler.PreHandleReceivedMessage(Client.Client client, ref Message message) => false;

        void IHandler.Tick(Client.Client client) { }
    }
}
