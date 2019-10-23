using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.Server.UsageMonitors
{
    /// <summary>
    /// Implementation of <see cref="IServerUsageMonitor"/> that limits CPU usage to a given value.
    /// </summary>
    public class CPUUsageLimiter : IServerUsageMonitor
    {
        private double cpuUsage;
        private volatile bool polling = false;
        private int cpuUsageLimit = 60;

        /// <summary>
        /// Set the CPU usage limit. Bounded between 0 and  100%.
        /// </summary>
        public int CPUUsageLimit { get => cpuUsageLimit; 
            set 
            {
                cpuUsageLimit = Math.Max(Math.Min(value, 100), 0);
            }
        }
        /// <summary>
        /// Sets the <see cref="CPUUsageLimit"/> value.
        /// </summary>
        /// <param name="cpuUsageLimit">Value to be set.</param>
        /// <returns>This <see cref="CPUUsageLimiter"/></returns>
        public CPUUsageLimiter SetCPUUsageLimit(int cpuUsageLimit)
        {
            CPUUsageLimit = cpuUsageLimit;
            return this;
        }

        void IServerUsageMonitor.Tick(Server server)
        {
            PollCPUUsage();
            while(cpuUsage > cpuUsageLimit)
            {
                server.LogDebug($"Throttling server due to CPUUsage = {cpuUsage}%!");
                Thread.Sleep(10);
                PollCPUUsage();
            }
        }

        private async void PollCPUUsage()
        {
            if (!polling)
            {
                polling = true;
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                await Task.Delay(500);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
                cpuUsage = cpuUsageTotal * 100;
                polling = false;
            }
        }
    }
}
