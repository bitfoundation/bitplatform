using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Bit.OwinCore.Middlewares
{
    public class AddRequiredHeadersIfNotAnyAspNetCoreMiddleware
    {
        private readonly RequestDelegate _next;

        public AddRequiredHeadersIfNotAnyAspNetCoreMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            // See https://github.com/odata/odata.net/issues/165

            if (!httpContext.Request.Headers.ContainsKey("Accept-Charset"))
                httpContext.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            if (!httpContext.Request.Headers.ContainsKey("X-Correlation-ID"))
                httpContext.Request.Headers.Add("X-Correlation-ID", new[] { Guid.NewGuid().ToString() });

            return _next(httpContext);
        }
    }
}
