﻿using MTSC.Common;
using System;
using System.Collections.Generic;

namespace MTSC.ServerSide.Schedulers
{
    public sealed class SequentialProcessingScheduler : IScheduler
    {
        public void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            foreach(var tuple in clientsQueues)
            {
                (var client, var messageQueue) = tuple;
                messageHandlingProcedure.Invoke(client, messageQueue);
            }
        }
    }
}
