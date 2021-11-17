using System.Threading.Tasks;
using Autofac;
using Bit.Core.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Owin.Middlewares
{
    public class DisposePipelineAwareDisposablesMiddleware
    {
        private readonly RequestDelegate _next;

        public DisposePipelineAwareDisposablesMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            httpContext.Response.OnStarting(async () =>
            {
                var lifetimeScope = httpContext.RequestServices.GetRequiredService<ILifetimeScope>();
                var dependencyManager = new AutofacDependencyManager();
                dependencyManager.UseContainer(lifetimeScope);
                foreach (var pipelineAwareDisposable in dependencyManager.GetPipelineAwareDisposables())
                {
                    await pipelineAwareDisposable.WaitForDisposal(httpContext.RequestAborted);
                }
            });

            await _next.Invoke(httpContext);
        }
    }
}
