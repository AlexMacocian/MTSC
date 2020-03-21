using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public class FireTasksAndForgetScheduler : IScheduler
    {
        public void ScheduleHandling(IProducerConsumerCollection<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            while(inQueue.TryTake(out var tuple))
            {
                (var client, var message) = tuple;
                Task.Run(() => { messageHandlingProcedure.Invoke(client, message); });
            }
        }
    }
}
