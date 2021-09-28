using System;
using System.Threading;

namespace MTSC.ServerSide.UsageMonitors
{
    /// <summary>
    /// Implementation of <see cref="IServerUsageMonitor"/> that enforces a specified tickrate.
    /// </summary>
    public sealed class TickrateEnforcer : IServerUsageMonitor
    {
        private DateTime lastTickTime = DateTime.Now;

        /// <summary>
        /// Set to true if the handler should post messages each tick.
        /// </summary>
        public bool Silent { get; set; }

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
            this.TicksPerSecond = ticksPerSecond;
            return this;
        }

        /// <summary>
        /// Sets the silent property
        /// </summary>
        /// <param name="silent"></param>
        /// <returns></returns>
        public TickrateEnforcer SetSilent(bool silent)
        {
            this.Silent = silent;
            return this;
        }

        void IServerUsageMonitor.Tick(Server server)
        {
            if((DateTime.Now - this.lastTickTime).TotalMilliseconds < 1000f / this.TicksPerSecond)
            {
                var sleepTime = (int)Math.Ceiling(Math.Max(1000f / this.TicksPerSecond - (DateTime.Now - this.lastTickTime).TotalMilliseconds, 0)) + 1;
                if(!this.Silent)
                {
                    server.LogDebug($"Sleeping thread for {sleepTime} ms due to throttling!");
                }

                Thread.Sleep(sleepTime);
            }

            this.lastTickTime = DateTime.Now;
        }
    }
}
