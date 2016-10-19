using System.Threading.Tasks;
using Microsoft.Owin;
using System.Linq;
using System;

namespace Foundation.Api.Middlewares
{
    public class OwinCacheResponseMiddleware : OwinMiddleware
    {
        public OwinCacheResponseMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Response.Headers.All(h => !string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=31536000" });
            if (context.Response.Headers.All(h => !string.Equals(h.Key, "Pragma", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Add("Pragma", new[] { "public" });

            await Next.Invoke(context);
        }
    }
}