using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Owin;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Owin.Middlewares
{
    public class IndexPageMiddleware
    {
        private readonly RequestDelegate _next;

        public IndexPageMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            string htmlPage = await File.ReadAllTextAsync(context.RequestServices.GetService<IPathProvider>().MapStaticFilePath(context.RequestServices.GetService<AppEnvironment>().GetConfig(AppEnvironment.KeyValues.IndexPagePath, AppEnvironment.KeyValues.IndexPagePathDefaultValue)!)).ConfigureAwait(false);

            string indexPageContents = await context.RequestServices.GetService<IHtmlPageProvider>().GetHtmlPageAsync(htmlPage, context.RequestAborted).ConfigureAwait(false);

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync(indexPageContents, context.RequestAborted).ConfigureAwait(false);
        }
    }
}
