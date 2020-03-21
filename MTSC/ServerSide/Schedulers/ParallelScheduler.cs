using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public class ParallelScheduler : IScheduler
    {
        void IScheduler.ScheduleHandling(IProducerConsumerCollection<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            List<Action> actionList = new List<Action>(inQueue.Count);

            while(inQueue.TryTake(out var tuple))
            {
                (var client, var message) = tuple;
                actionList.Add(new Action(() => { messageHandlingProcedure.Invoke(client, message); }));
            }
            Parallel.Invoke(actionList.ToArray());
        }
    }
}
