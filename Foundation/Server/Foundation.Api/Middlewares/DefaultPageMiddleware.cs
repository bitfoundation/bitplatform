using System.Threading;
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
            IDependencyResolver dependecyResolver = context.GetDependencyResolver();

            string defaultPage = await dependecyResolver.Resolve<IDefaultHtmlPageProvider>().GetDefaultPageAsync(CancellationToken.None);

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync(defaultPage);
        }
    }
}