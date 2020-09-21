using Bit.Owin.Implementations;
using Microsoft.Owin;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public override Task OnActionExecutingAsync(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Response.Headers.Any(h => string.Equals(h.Key, "Cache-Control", StringComparison.InvariantCultureIgnoreCase)))
                context.Response.Headers.Remove("Cache-Control");
            context.Response.Headers.Add("Cache-Control", new[] { "no-store, no-transform" });

            return base.OnActionExecutingAsync(context);
        }
    }
}
