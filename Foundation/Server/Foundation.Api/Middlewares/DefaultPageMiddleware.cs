using System.Threading.Tasks;
using Microsoft.Owin;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;

namespace Foundation.Api.Middlewares
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