using Foundation.AspNetCore.Contracts;
using Foundation.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Foundation.AspNetCore.Test.Api.Middlewares
{
    public class TestWebApiCoreMvcMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            aspNetCoreApp.Map("/api", innerAspNetCoreApp =>
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
