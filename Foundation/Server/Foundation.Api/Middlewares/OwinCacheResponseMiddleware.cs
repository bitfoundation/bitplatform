using System.Threading.Tasks;
using Microsoft.Owin;
using System.Linq;
using System;
using Foundation.Api.Implementations;
using System.Threading;

namespace Foundation.Api.Middlewares
{
    public class OwinCacheResponseMiddleware : DefaultOwinActionFilterMiddleware
    {
        public OwinCacheResponseMiddleware()
        {

        }

        public OwinCacheResponseMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task OnActionExecutingAsync(IOwinContext owinContext, CancellationToken cancellationToken)
        {
            if (owinContext.Response.Headers.Any(h => string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                owinContext.Response.Headers.Remove("Cache-Control");
            owinContext.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=31536000" });

            if (owinContext.Response.Headers.Any(h => string.Equals(h.Key, "Pragma", StringComparison.InvariantCultureIgnoreCase)))
                owinContext.Response.Headers.Remove("Pragma");
            owinContext.Response.Headers.Add("Pragma", new[] { "public" });

            if (owinContext.Response.Headers.Any(h => string.Equals(h.Key, "Expires", StringComparison.InvariantCultureIgnoreCase)))
                owinContext.Response.Headers.Remove("Expires");
            owinContext.Response.Headers.Add("Expires", new[] { "31536000" });

            await base.OnActionExecutingAsync(owinContext, cancellationToken);
        }
    }
}