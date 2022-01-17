using MTSC.Common;
using MTSC.ServerSide.BackgroundServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public sealed class FireTasksAndForgetScheduler : IScheduler
    {
        public void ScheduleBackgroundService(BackgroundServiceBase backgroundServiceBase)
        {
            Task.Run(() => backgroundServiceBase.Execute());
        }

        public void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            foreach(var tuple in clientsQueues)
            {
                (var client, var messageQueue) = tuple;
                Task.Run(() => messageHandlingProcedure.Invoke(client, messageQueue));
            }
        }
    }
}
