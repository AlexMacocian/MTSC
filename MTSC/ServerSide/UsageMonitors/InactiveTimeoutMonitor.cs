using System;
using System.Linq;

namespace MTSC.ServerSide.UsageMonitors
{
    public sealed class InactiveTimeoutMonitor : IServerUsageMonitor
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        public InactiveTimeoutMonitor WithTimeout(TimeSpan timeout)
        {
            this.Timeout = timeout;
            return this;
        }

        public void Tick(Server server)
        {
            var procTime = DateTime.Now;
            foreach(var client in server.Clients)
            {
                if (procTime - client.LastActivityTime > this.Timeout)
                {
                    client.ToBeRemoved = true;
                }
            }
        }
    }
}
