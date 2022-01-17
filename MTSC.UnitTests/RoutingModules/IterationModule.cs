using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.UnitTests.BackgroundServices;
using System;
using System.Threading.Tasks;

namespace MTSC.UnitTests.RoutingModules
{
    public sealed class IterationModule : HttpRouteBase
    {
        private readonly IteratingService iteratingService;

        public IterationModule(
            IteratingService iteratingService)
        {
            this.iteratingService = iteratingService ?? throw new ArgumentNullException(nameof(iteratingService));
        }

        public override Task<HttpResponse> HandleRequest(HttpRequestContext request)
        {
            var iteration = this.iteratingService.Iteration;

            return Task.FromResult(new HttpResponse
            {
                StatusCode = HttpMessage.StatusCodes.OK,
                BodyString = iteration.ToString()
            });
        }
    }
}
