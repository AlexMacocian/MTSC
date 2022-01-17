using System;

namespace MTSC.ServerSide.BackgroundServices
{
    /// <summary>
    /// Base class for background services.
    /// These services will be called periodically by the server, the period being defined by the <see cref="BackgroundServiceBase.ActivationInterval"/>.
    /// </summary>
    public abstract class BackgroundServiceBase : ISetBackgroundServiceProperties
    {
        /// <summary>
        /// <see cref="ServerSide.Server"/> containing the <see cref="BackgroundServiceBase"/>.
        /// </summary>
        public Server Server { get; private set; }
        /// <summary>
        /// Interval between calls from server.
        /// </summary>
        public TimeSpan ActivationInterval { get; private set; }
        /// <summary>
        /// Last time the <see cref="BackgroundServiceBase"/> was called.
        /// </summary>
        public DateTime LastActivationTime { get; private set; }

        public abstract void Execute();

        void ISetBackgroundServiceProperties.SetActivationInterval(TimeSpan activationInterval)
        {
            this.ActivationInterval = activationInterval;
        }
        void ISetBackgroundServiceProperties.SetLastActivationTime(DateTime dateTime)
        {
            this.LastActivationTime = dateTime;
        }
        void ISetBackgroundServiceProperties.SetServer(Server server)
        {
            this.Server = server ?? throw new ArgumentNullException(nameof(server));
        }
    }
}
