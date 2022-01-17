using System;

namespace MTSC.ServerSide.BackgroundServices
{
    internal class BackgroundServiceMetadata
    {
        public DateTime LastActivationTime { get; set; }
        public TimeSpan ActivationInterval { get; set; }
        public Type RegisteredType { get; set; }
        public BackgroundServiceBase InitializedService { get; set; }
    }
}
