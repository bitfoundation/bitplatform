using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreReadAuthTokenFromCookieMiddleware
    {
        private readonly RequestDelegate _next;

        public AspNetCoreReadAuthTokenFromCookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Headers?.ContainsKey("Authorization") == false && context.Request.Cookies?["access_token"] != null)
            {
                context.Request.Headers.Add("Authorization", new[]  {
                    $"{context.Request.Cookies["token_type"]} {context.Request.Cookies["access_token"]}"
                });
            }

            return _next.Invoke(context);
        }
    }
}
