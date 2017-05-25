using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreNoCacheResponseMiddleware
    {
        private readonly RequestDelegate Next;

        public AspNetCoreNoCacheResponseMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Response.Headers.All(h => !string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Add("Cache-Control", new[] { "no-cache, no-store, must-revalidate" });
            if (context.Response.Headers.All(h => !string.Equals(h.Key, "Pragma", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Add("Pragma", new[] { "no-cache" });
            if (context.Response.Headers.All(h => !string.Equals(h.Key, "Expires", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Add("Expires", new[] { "0" });

            await Next.Invoke(context);
        }
    }
}
