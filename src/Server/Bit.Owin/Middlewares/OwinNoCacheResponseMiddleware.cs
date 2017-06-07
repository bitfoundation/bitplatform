using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.Owin.Implementations;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class OwinNoCacheResponseMiddleware : DefaultOwinActionFilterMiddleware
    {
        public OwinNoCacheResponseMiddleware()
        {

        }

        public OwinNoCacheResponseMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task OnActionExecutingAsync(IOwinContext owinContext)
        {
            if (owinContext.Response.Headers.Any(h => string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                owinContext.Response.Headers.Remove("Cache-Control");
            owinContext.Response.Headers.Add("Cache-Control", new[] { "no-cache, no-store, must-revalidate" });

            if (owinContext.Response.Headers.Any(h => string.Equals(h.Key, "Pragma", StringComparison.InvariantCultureIgnoreCase)))
                owinContext.Response.Headers.Remove("Pragma");
            owinContext.Response.Headers.Add("Pragma", new[] { "no-cache" });

            if (owinContext.Response.Headers.Any(h => string.Equals(h.Key, "Expires", StringComparison.InvariantCultureIgnoreCase)))
                owinContext.Response.Headers.Remove("Expires");
            owinContext.Response.Headers.Add("Expires", new[] { "0" });

            await base.OnActionExecutingAsync(owinContext);
        }
    }
}
