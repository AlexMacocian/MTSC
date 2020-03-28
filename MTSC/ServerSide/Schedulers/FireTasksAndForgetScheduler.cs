using MTSC.Common;
using System;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public class FireTasksAndForgetScheduler : IScheduler
    {
        public void ScheduleHandling(IConsumerQueue<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure)
        {
            while(inQueue.TryDequeue(out var tuple))
            {
                (var client, var message) = tuple;
                Task.Run(() => { messageHandlingProcedure.Invoke(client, message); });
            }
        }
    }
}
