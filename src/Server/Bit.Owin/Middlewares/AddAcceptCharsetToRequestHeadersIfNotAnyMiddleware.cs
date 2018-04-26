using Microsoft.Owin;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    /// <summary>
    /// See https://github.com/odata/odata.net/issues/165
    /// </summary>
    public class AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware : OwinMiddleware
    {
        public AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware(OwinMiddleware next)
           : base(next)
        {

        }

        public override Task Invoke(IOwinContext context)
        {
            if (!context.Request.Headers.ContainsKey("Accept-Charset"))
                context.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            return Next.Invoke(context);
        }
    }
}
