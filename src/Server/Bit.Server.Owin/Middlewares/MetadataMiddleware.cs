using Bit.Core.Contracts;
using Bit.Owin.Contracts.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Owin.Middlewares
{
    public class MetadataMiddleware
    {
        private readonly RequestDelegate _next;

        public MetadataMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IContentFormatter contentFormatter =
                context.RequestServices.GetService<IContentFormatter>();

            IAppMetadataProvider appMetadataProvider =
                context.RequestServices.GetService<IAppMetadataProvider>();

            context.Response.ContentType = "application/json; charset=utf-8";

            await context.Response.WriteAsync(contentFormatter.Serialize(await appMetadataProvider.GetAppMetadata().ConfigureAwait(false)), context.RequestAborted).ConfigureAwait(false);
        }
    }
}
