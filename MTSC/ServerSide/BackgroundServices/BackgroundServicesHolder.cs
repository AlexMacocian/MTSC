using System;
using System.Collections.Generic;

namespace MTSC.ServerSide.BackgroundServices
{
    public sealed class BackgroundServicesHolder
    {
        private readonly List<BackgroundServiceMetadata> backgroundServices = new();
        private readonly Server server;

        public BackgroundServicesHolder(Server server)
        {
            this.server = server ?? throw new ArgumentNullException();
        }

        public void RegisterBackgroundService<T>(TimeSpan activationInterval)
            where T : BackgroundServiceBase
        {
            this.server.ServiceManager.RegisterSingleton<T>();
            this.backgroundServices.Add(new BackgroundServiceMetadata { ActivationInterval = activationInterval, RegisteredType = typeof(T) });
        }

        public void Initialize()
        {
            foreach(var backgroundServiceMetadata in this.backgroundServices)
            {
                this.server.Log($"Initializing background service {backgroundServiceMetadata.RegisteredType.Name}");
                backgroundServiceMetadata.InitializedService = this.server.ServiceManager.GetService(backgroundServiceMetadata.RegisteredType) as BackgroundServiceBase;
                (backgroundServiceMetadata.InitializedService as ISetBackgroundServiceProperties).SetServer(this.server);
                (backgroundServiceMetadata.InitializedService as ISetBackgroundServiceProperties).SetActivationInterval(backgroundServiceMetadata.ActivationInterval);
            }
        }

        public void Tick()
        {
            foreach(var backgroundServiceMetadata in this.backgroundServices)
            {
                if (backgroundServiceMetadata.InitializedService is null)
                {
                    continue;
                }

                if (DateTime.Now - backgroundServiceMetadata.LastActivationTime > backgroundServiceMetadata.ActivationInterval)
                {
                    this.server.Log($"Activating background service {backgroundServiceMetadata.RegisteredType.Name}");
                    var activationTime = DateTime.Now;
                    backgroundServiceMetadata.LastActivationTime = activationTime;
                    (backgroundServiceMetadata.InitializedService as ISetBackgroundServiceProperties).SetLastActivationTime(activationTime);
                    this.server.Scheduler.ScheduleBackgroundService(backgroundServiceMetadata.InitializedService);
                }
            }
        }
    }
}
