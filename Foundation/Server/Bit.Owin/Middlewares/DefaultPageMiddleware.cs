using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class DefaultPageMiddleware : OwinMiddleware
    {
        public DefaultPageMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            string defaultPage = await dependencyResolver.Resolve<IDefaultHtmlPageProvider>().GetDefaultPageAsync(context.Request.CallCancelled);

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync(defaultPage, context.Request.CallCancelled);
        }
    }
}