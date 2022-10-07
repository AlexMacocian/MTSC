using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;

namespace MTSC.UnitTests.Models;

public sealed class ServiceOnInitialization : IHandler, IRunOnInitialization
{
    public bool RanOnInitialization { get; private set; }

    public void OnInitialization(Server server)
    {
        if (this.RanOnInitialization)
        {
            throw new InvalidOperationException("Initialization procedure already ran");
        }

        this.RanOnInitialization = true;
    }

    public void ClientRemoved(Server server, ClientData client)
    {
    }

    public bool HandleClient(Server server, ClientData client) => true;

    public bool HandleReceivedMessage(Server server, ClientData client, Message message) => false;

    public bool HandleSendMessage(Server server, ClientData client, ref Message message) => false;

    public bool PreHandleReceivedMessage(Server server, ClientData client, ref Message message) => false;

    public void Tick(Server server)
    {
    }
}
