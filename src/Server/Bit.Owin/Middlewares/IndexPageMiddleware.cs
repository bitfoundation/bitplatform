using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.Owin;
using System.IO;
using System.Threading.Tasks;

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

            string htmlPage = File.ReadAllText(dependencyResolver.Resolve<IPathProvider>().StaticFileMapPath(dependencyResolver.Resolve<AppEnvironment>().GetConfig("IndexPagePath", "indexPage.html")));

            string indexPageContents = await dependencyResolver.Resolve<IHtmlPageProvider>().GetHtmlPageAsync(htmlPage, context.Request.CallCancelled);

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync(indexPageContents, context.Request.CallCancelled);
        }
    }
}