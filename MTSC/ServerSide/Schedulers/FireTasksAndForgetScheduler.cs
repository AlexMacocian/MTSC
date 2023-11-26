using MTSC.Common;
using MTSC.ServerSide.BackgroundServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public sealed class FireTasksAndForgetScheduler : IScheduler
    {
        private readonly TaskFactory taskFactory = new TaskFactory();

        public void ScheduleBackgroundService(BackgroundServiceBase backgroundServiceBase)
        {
            this.taskFactory.StartNew(() => backgroundServiceBase.Execute(), TaskCreationOptions.LongRunning);
        }

        public void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            foreach(var tuple in clientsQueues)
            {
                (var client, var messageQueue) = tuple;
                this.taskFactory.StartNew(() => messageHandlingProcedure.Invoke(client, messageQueue), TaskCreationOptions.LongRunning);
            }
        }
    }
}
