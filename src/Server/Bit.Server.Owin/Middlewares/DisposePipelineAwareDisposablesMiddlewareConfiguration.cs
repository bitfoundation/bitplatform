using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Builder;

namespace Bit.Owin.Middlewares
{
    public class DisposePipelineAwareDisposablesMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public void Configure(IApplicationBuilder aspNetCoreApp)
        {
            aspNetCoreApp.UseMiddleware<DisposePipelineAwareDisposablesMiddleware>();
        }
    }
}
