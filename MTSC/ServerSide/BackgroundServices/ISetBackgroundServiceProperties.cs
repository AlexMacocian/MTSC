using System;

namespace MTSC.ServerSide.BackgroundServices
{
    internal interface ISetBackgroundServiceProperties
    {
        void SetLastActivationTime(DateTime dateTime);
        void SetServer(Server server);
        void SetActivationInterval(TimeSpan activationInterval);
    }
}
