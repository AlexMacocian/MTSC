using MTSC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Schedulers
{
    public sealed class ParallelScheduler : IScheduler
    {
        public void ScheduleHandling(List<(ClientData, IConsumerQueue<Message>)> clientsQueues, Action<ClientData, IConsumerQueue<Message>> messageHandlingProcedure)
        {
            Parallel.Invoke(clientsQueues.Select(tuple => new Action(() => messageHandlingProcedure.Invoke(tuple.Item1, tuple.Item2))).ToArray());
        }
    }
}
