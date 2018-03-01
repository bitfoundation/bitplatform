using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class IndexPageMiddleware : OwinMiddleware
    {
        public IndexPageMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            string indexPageContents = await dependencyResolver.Resolve<IIndexPageContentsProvider>().GetIndexPageHtmlContentsAsync(context.Request.CallCancelled);

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync(indexPageContents, context.Request.CallCancelled);
        }
    }
}