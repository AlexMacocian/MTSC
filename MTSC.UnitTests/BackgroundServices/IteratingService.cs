using MTSC.ServerSide;

namespace MTSC.UnitTests.BackgroundServices
{
    public class IteratingService
    {
        private readonly Server server;

        public int Iteration { get; private set; } = 0;

        public IteratingService(Server server)
        {
            this.server = server;
        }

        public void Iterate()
        {
            this.server.Log("Iterating");
            this.Iteration++;
            this.server.Log("Iterated");
        }
    }
}
