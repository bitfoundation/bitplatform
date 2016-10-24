using Microsoft.Owin;
using System.Threading.Tasks;

namespace Foundation.Api.Middlewares
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

        public override async Task Invoke(IOwinContext context)
        {
            if (!context.Request.Headers.ContainsKey("Accept-Charset"))
                context.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            await Next.Invoke(context);
        }
    }
}
