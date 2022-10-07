using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.UnitTests.Models;

public sealed class ServiceOnStartup : IHandler, IRunOnStartup
{
    public int RanOnStartup { get; set; } = 0;

    public void OnStartup(Server server)
    {
        this.RanOnStartup += 1;
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
