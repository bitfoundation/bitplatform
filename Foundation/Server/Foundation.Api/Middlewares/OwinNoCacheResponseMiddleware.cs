using System.Threading.Tasks;
using Microsoft.Owin;
using System.Linq;
using System;

namespace Foundation.Api.Middlewares
{
    public class OwinNoCacheResponseMiddleware : OwinMiddleware
    {
        public OwinNoCacheResponseMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task Invoke(IOwinContext context)
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
