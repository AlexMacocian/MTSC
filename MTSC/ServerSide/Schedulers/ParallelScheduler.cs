using MTSC.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public class ParallelScheduler : IScheduler
    {
        void IScheduler.ScheduleHandling(IConsumerQueue<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            List<Action> actionList = new List<Action>();

            while(inQueue.TryDequeue(out var tuple))
            {
                (var client, var message) = tuple;
                actionList.Add(new Action(() => { messageHandlingProcedure.Invoke(client, message); }));
            }
            Parallel.Invoke(actionList.ToArray());
        }
    }
}
