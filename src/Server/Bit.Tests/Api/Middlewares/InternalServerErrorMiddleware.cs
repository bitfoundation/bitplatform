using Microsoft.Owin;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares
{
    public class InternalServerErrorMiddleware : OwinMiddleware
    {
        public InternalServerErrorMiddleware(OwinMiddleware next) :
            base(next)
        {

        }

        public override Task Invoke(IOwinContext context)
        {
            context.Response.StatusCode = 501;
            return context.Response.WriteAsync("NotImplemented");
        }
    }
}
