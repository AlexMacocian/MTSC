using MTSC.Common;
using System;

namespace MTSC.ServerSide.Schedulers
{
    public interface IScheduler
    {
        void ScheduleHandling(IConsumerQueue<(ClientData, Message)> inQueue, Action<ClientData, Message> messageHandlingProcedure);
    }
}
