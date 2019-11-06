using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Server.UsageMonitors
{
    /// <summary>
    /// Interface for server usage monitors.
    /// </summary>
    public interface IServerUsageMonitor
    {
        /// <summary>
        /// Called each server tick.
        /// This method determines the usage of the server as well as behaviour.
        /// </summary>
        /// <remarks>
        /// This is useful in case the server needs a forced tickrate or should lower CPU usage when not under heavy load.
        /// Sleeping the current thread in this method will sleep the server thread.
        /// </remarks>
        /// <param name="server"></param>
        void Tick(Server server);
    }
}
