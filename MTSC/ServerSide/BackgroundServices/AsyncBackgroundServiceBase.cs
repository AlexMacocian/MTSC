using System.Threading.Tasks;

namespace MTSC.ServerSide.BackgroundServices
{
    public abstract class AsyncBackgroundServiceBase : BackgroundServiceBase
    {
        public override sealed void Execute()
        {
            var task = Task.Run(this.ExecuteAsync);
            Task.WaitAll(task);
        }

        public abstract Task ExecuteAsync();
    }
}
