using Bit.OwinCore.Contracts;
using Bit.OwinCore.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Bit.Tests.Api.Middlewares
{
    public class TestWebApiCoreMvcMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            aspNetCoreApp.Map("/api-core", innerAspNetCoreApp =>
            {
                innerAspNetCoreApp.UseMvcWithDefaultRoute();

                innerAspNetCoreApp.UseMiddleware<AspNetCoreNoCacheResponseMiddleware>();
            });
        }

        public virtual RegisterKind GetRegisterKind()
        {
            return RegisterKind.AfterOwinPipeline;
        }
    }
}
