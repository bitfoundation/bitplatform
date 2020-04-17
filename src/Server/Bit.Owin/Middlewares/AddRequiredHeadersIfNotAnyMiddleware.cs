using Microsoft.Owin;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class AddRequiredHeadersIfNotAnyMiddleware : OwinMiddleware
    {
        public AddRequiredHeadersIfNotAnyMiddleware(OwinMiddleware next)
           : base(next)
        {

        }

        public override Task Invoke(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // See https://github.com/odata/odata.net/issues/165
            if (!context.Request.Headers.ContainsKey("Accept-Charset"))
                context.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
                context.Request.Headers.Add("X-Correlation-ID", new[] { Guid.NewGuid().ToString() });

            return Next.Invoke(context);
        }
    }
}
