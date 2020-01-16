using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreNoCacheResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public AspNetCoreNoCacheResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Cache-Control");
            context.Response.Headers.Add("Cache-Control", new[] { "no-store, no-transform" });

            return _next.Invoke(context);
        }
    }
}
