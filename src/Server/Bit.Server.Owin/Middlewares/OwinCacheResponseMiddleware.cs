using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.Owin.Implementations;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
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

        public override Task OnActionExecutingAsync(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Cache-Control");
            context.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=31536000" });

            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Pragma", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Pragma");
            context.Response.Headers.Add("Pragma", new[] { "public" });

            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Expires", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Expires");
            context.Response.Headers.Add("Expires", new[] { "max" });

            return base.OnActionExecutingAsync(context);
        }
    }
}