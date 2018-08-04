using Microsoft.Owin;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class ReadAuthTokenFromCookieMiddleware : OwinMiddleware
    {
        public ReadAuthTokenFromCookieMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            if (context.Request.Headers?.ContainsKey("Authorization") == false && context.Request.Cookies?["access_token"] != null)
            {
                context.Request.Headers.Add("Authorization", new[] {
                    $"{context.Request.Cookies["token_type"]} {context.Request.Cookies["access_token"]}"
                });
            }

            return Next.Invoke(context);
        }
    }
}
