using MTSC.Common;
using System;
using System.Collections.Concurrent;

namespace MTSC.ServerSide.Schedulers
{
    public class SequentialProcessingScheduler : IScheduler
    {
        void IScheduler.ScheduleHandling(IConsumerQueue<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            while (inQueue.TryDequeue(out var tuple))
            {
                (var client, var message) = tuple;
                messageHandlingProcedure.Invoke(client, message);
            }
        }
    }
}
