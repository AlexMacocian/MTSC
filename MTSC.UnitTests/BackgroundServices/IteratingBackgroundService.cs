using MTSC.ServerSide.BackgroundServices;

namespace MTSC.UnitTests.BackgroundServices
{
    public sealed class IteratingBackgroundService : BackgroundServiceBase
    {
        private readonly IteratingService iteratingService;

        public IteratingBackgroundService(IteratingService iteratingService)
        {
            this.iteratingService = iteratingService;
        }

        public override void Execute()
        {
            this.iteratingService.Iterate();
        }
    }
}
