using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.ServerSide.UsageMonitors
{
    /// <summary>
    /// Implementation of <see cref="IServerUsageMonitor"/> that limits CPU usage to a given value.
    /// </summary>
    public sealed class CPUUsageLimiter : IServerUsageMonitor
    {
        private double cpuUsage;
        private volatile bool polling = false;
        private int cpuUsageLimit = 60;

        /// <summary>
        /// Set the CPU usage limit. Bounded between 0 and  100%.
        /// </summary>
        public int CPUUsageLimit { get => this.cpuUsageLimit; 
            set 
            {
                this.cpuUsageLimit = Math.Max(Math.Min(value, 100), 0);
            }
        }
        /// <summary>
        /// Sets the <see cref="CPUUsageLimit"/> value.
        /// </summary>
        /// <param name="cpuUsageLimit">Value to be set.</param>
        /// <returns>This <see cref="CPUUsageLimiter"/></returns>
        public CPUUsageLimiter SetCPUUsageLimit(int cpuUsageLimit)
        {
            this.CPUUsageLimit = cpuUsageLimit;
            return this;
        }

        void IServerUsageMonitor.Tick(Server server)
        {
            this.PollCPUUsage();
            while(this.cpuUsage > this.cpuUsageLimit)
            {
                server.LogDebug($"Throttling server due to CPUUsage = {this.cpuUsage}%!");
                Thread.Sleep(10);
                this.PollCPUUsage();
            }
        }

        private async void PollCPUUsage()
        {
            if (!this.polling)
            {
                this.polling = true;
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                await Task.Delay(500);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
                this.cpuUsage = cpuUsageTotal * 100;
                this.polling = false;
            }
        }
    }
}
