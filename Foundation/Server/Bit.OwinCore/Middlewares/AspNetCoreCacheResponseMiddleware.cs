using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreCacheResponseMiddleware
    {
        private readonly RequestDelegate Next;

        public AspNetCoreCacheResponseMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Response.Headers.All(h => !string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=31536000" });
            if (context.Response.Headers.All(h => !string.Equals(h.Key, "Pragma", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Add("Pragma", new[] { "public" });

            await Next.Invoke(context);
        }
    }
}
