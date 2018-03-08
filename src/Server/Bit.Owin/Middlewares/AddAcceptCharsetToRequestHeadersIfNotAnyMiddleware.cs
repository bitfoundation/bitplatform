using Microsoft.Owin;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    /// <summary>
    /// See https://github.com/odata/odata.net/issues/165
    /// </summary>
    public class AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware : OwinMiddleware
    {
        protected AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware(OwinMiddleware next)
           : base(next)
        {

        }

        public async override Task Invoke(IOwinContext context)
        {
            if (!context.Request.Headers.ContainsKey("Accept-Charset"))
                context.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            await Next.Invoke(context);
        }
    }
}
