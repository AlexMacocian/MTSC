﻿using MTSC.Common;
using MTSC.ServerSide.BackgroundServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public sealed class TaskAwaiterScheduler : IScheduler
    {
        public void ScheduleBackgroundService(BackgroundServiceBase backgroundServiceBase)
        {
            (backgroundServiceBase as ISetBackgroundServiceProperties).SetLastActivationTime(DateTime.Now);
            var task = Task.Run(() => backgroundServiceBase.Execute());
        }

        public void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            var tasks = new List<Task>();
            foreach(var tuple in clientsQueues)
            {
                (var client, var messageQueue) = tuple;
                tasks.Add(Task.Run(() => messageHandlingProcedure.Invoke(client, messageQueue)));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}
