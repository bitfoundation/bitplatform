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

        public async Task Invoke(HttpContext context)
        {
            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Cache-Control");
            context.Response.Headers.Add("Cache-Control", new[] { "no-cache, no-store, must-revalidate" });

            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Pragma", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Pragma");
            context.Response.Headers.Add("Pragma", new[] { "no-cache" });

            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Expires", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Expires");
            context.Response.Headers.Add("Expires", new[] { "0" });

            await _next.Invoke(context);
        }
    }
}
