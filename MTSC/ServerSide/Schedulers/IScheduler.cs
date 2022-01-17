using MTSC.Common;
using MTSC.ServerSide.BackgroundServices;
using System;
using System.Collections.Generic;

namespace MTSC.ServerSide.Schedulers
{
    public interface IScheduler
    {
        void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure);
        void ScheduleBackgroundService(BackgroundServiceBase backgroundServiceBase);
    }
}
