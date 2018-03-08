using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Bit.OwinCore.Middlewares
{
    /// <summary>
    /// See https://github.com/odata/odata.net/issues/165
    /// </summary>
    public class AddAcceptCharsetToRequestHeadersIfNotAnyAspNetCoreMiddleware
    {
        private readonly RequestDelegate _next;

        public AddAcceptCharsetToRequestHeadersIfNotAnyAspNetCoreMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!httpContext.Request.Headers.ContainsKey("Accept-Charset"))
                httpContext.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            await _next(httpContext);
        }
    }
}
