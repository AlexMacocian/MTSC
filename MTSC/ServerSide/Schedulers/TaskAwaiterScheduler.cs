using MTSC.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public class TaskAwaiterScheduler : IScheduler
    {
        void IScheduler.ScheduleHandling(IConsumerQueue<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            List<Task> tasks = new List<Task>();
            while(inQueue.TryDequeue(out var tuple))
            {
                (var client, var message) = tuple;
                tasks.Add(Task.Run(() => messageHandlingProcedure.Invoke(client, message)));
            }
            Task.WaitAll(tasks.ToArray());
        }
    }
}
