using MTSC.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MTSC.ServerSide.Schedulers
{
    public sealed class MultiThreadScheduler : IScheduler
    {
        public void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            foreach((var client, var messageQueue) in clientsQueues)
            {
                new Thread(() => 
                {
                    messageHandlingProcedure(client, messageQueue);
                }).Start();
            }
        }
    }
}
