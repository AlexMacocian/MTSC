﻿using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.ServerSide;
using System;
using System.Threading.Tasks;

namespace MTSC.UnitTests
{
    public class MultipartModule : HttpRouteBase
    {
        public override Task<HttpResponse> HandleRequest(HttpRequestContext request)
        {
            if (request.Request.Form.Count > 0)
            {
                return Task.FromResult(new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK });
            }

            return Task.FromResult(new HttpResponse { StatusCode = HttpMessage.StatusCodes.BadRequest });
        }
    }
}
