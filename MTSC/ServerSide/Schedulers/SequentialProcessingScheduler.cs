using System;
using System.Collections.Concurrent;

namespace MTSC.ServerSide.Schedulers
{
    public class SequentialProcessingScheduler : IScheduler
    {
        void IScheduler.ScheduleHandling(IProducerConsumerCollection<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            while (inQueue.TryTake(out var tuple))
            {
                (var client, var message) = tuple;
                messageHandlingProcedure.Invoke(client, message);
            }
        }
    }
}
