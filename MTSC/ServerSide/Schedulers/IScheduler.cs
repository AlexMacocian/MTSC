using System;
using System.Collections.Concurrent;

namespace MTSC.ServerSide.Schedulers
{
    public interface IScheduler
    {
        void ScheduleHandling(IProducerConsumerCollection<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure);
    }
}
