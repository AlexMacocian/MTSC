using MTSC.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public class ParallelScheduler : IScheduler
    {
        void IScheduler.ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            List<Action> actionList = new List<Action>();

            foreach(var tuple in clientsQueues)
            {
                (var client, var messageQueue) = tuple;
                actionList.Add(new Action(() => messageHandlingProcedure.Invoke(client, messageQueue)));
            }

            Parallel.Invoke(actionList.ToArray());
        }
    }
}
