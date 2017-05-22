using Foundation.Api.Implementations;
using Microsoft.Owin;
using System.Threading.Tasks;

namespace Foundation.Api.Middlewares
{
    /// <summary>
    /// See https://github.com/odata/odata.net/issues/165
    /// </summary>
    public class AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware : DefaultOwinActionFilterMiddleware
    {
        public AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware()
        {

        }

        public AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task OnActionExecutingAsync(IOwinContext owinContext)
        {
            if (!owinContext.Request.Headers.ContainsKey("Accept-Charset"))
                owinContext.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            await base.OnActionExecutingAsync(owinContext);
        }
    }
}
