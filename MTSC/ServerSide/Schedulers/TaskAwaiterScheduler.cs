using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    class TaskAwaiterScheduler : IScheduler
    {
        void IScheduler.ScheduleHandling(IProducerConsumerCollection<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            List<Task> tasks = new List<Task>();
            while(inQueue.TryTake(out var tuple))
            {
                (var client, var message) = tuple;
                tasks.Add(Task.Run(() => messageHandlingProcedure.Invoke(client, message)));
            }
            Task.WaitAll(tasks.ToArray());
        }
    }
}
