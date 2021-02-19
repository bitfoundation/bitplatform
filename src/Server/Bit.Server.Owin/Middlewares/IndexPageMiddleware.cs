using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.Owin;
using System;
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
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            string htmlPage = await File.ReadAllTextAsync(dependencyResolver.Resolve<IPathProvider>().MapStaticFilePath(dependencyResolver.Resolve<AppEnvironment>().GetConfig(AppEnvironment.KeyValues.IndexPagePath, AppEnvironment.KeyValues.IndexPagePathDefaultValue)!)).ConfigureAwait(false);

            string indexPageContents = await dependencyResolver.Resolve<IHtmlPageProvider>().GetHtmlPageAsync(htmlPage, context.Request.CallCancelled).ConfigureAwait(false);

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync(indexPageContents, context.Request.CallCancelled).ConfigureAwait(false);
        }
    }
}