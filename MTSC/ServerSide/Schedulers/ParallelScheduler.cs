using MTSC.Common;
using MTSC.ServerSide.BackgroundServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public sealed class ParallelScheduler : IScheduler
    {
        public void ScheduleBackgroundService(BackgroundServiceBase backgroundServiceBase)
        {
            Parallel.Invoke(backgroundServiceBase.Execute);
        }

        public void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            Parallel.Invoke(clientsQueues.Select(tuple => new Action(() => messageHandlingProcedure.Invoke(tuple.Item1, tuple.Item2))).ToArray());
        }
    }
}
