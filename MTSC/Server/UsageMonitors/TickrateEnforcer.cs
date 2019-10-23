using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MTSC.Server.UsageMonitors
{
    /// <summary>
    /// Implementation of <see cref="IServerUsageMonitor"/> that enforces a specified tickrate.
    /// </summary>
    public class TickrateEnforcer : IServerUsageMonitor
    {
        private DateTime lastTickTime = DateTime.Now;

        /// <summary>
        /// Number of server ticks in one second.
        /// </summary>
        public int TicksPerSecond { get; set; }
        /// <summary>
        /// Sets the <see cref="TicksPerSecond"/> value.
        /// </summary>
        /// <param name="ticksPerSecond">Value to be set.</param>
        /// <returns>This <see cref="TickrateEnforcer"/></returns>
        public TickrateEnforcer SetTicksPerSecond(int ticksPerSecond)
        {
            TicksPerSecond = ticksPerSecond;
            return this;
        }

        void IServerUsageMonitor.Tick(Server server)
        {
            if((DateTime.Now - lastTickTime).TotalMilliseconds < 1000f / TicksPerSecond)
            {
                int sleepTime = (int)Math.Ceiling(Math.Max(1000f / TicksPerSecond - (DateTime.Now - lastTickTime).TotalMilliseconds, 0)) + 1;
                server.LogDebug($"Sleeping thread for {sleepTime} ms due to throttling!");
                Thread.Sleep(sleepTime);
            }
            lastTickTime = DateTime.Now;
        }
    }
}
