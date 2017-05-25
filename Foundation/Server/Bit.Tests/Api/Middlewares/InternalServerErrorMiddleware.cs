using System.Threading.Tasks;
using Microsoft.Owin;

namespace Bit.Tests.Api.Middlewares
{
    public class InternalServerErrorMiddleware : OwinMiddleware
    {
        public InternalServerErrorMiddleware(OwinMiddleware next) :
            base(next)
        {

        }

        public override async Task Invoke(IOwinContext context)
        {
            context.Response.StatusCode = 501;

            await context.Response.WriteAsync("NotImplemented");
        }
    }
}
