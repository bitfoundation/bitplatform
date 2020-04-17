using Bit.Core.Contracts;
using Bit.Owin.Contracts.Metadata;
using Microsoft.Owin;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class MetadataMiddleware : OwinMiddleware
    {
        public MetadataMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IContentFormatter contentFormatter =
                context.GetDependencyResolver().Resolve<IContentFormatter>();

            IAppMetadataProvider appMetadataProvider =
                context.GetDependencyResolver().Resolve<IAppMetadataProvider>();

            context.Response.ContentType = "application/json; charset=utf-8";

            await context.Response.WriteAsync(contentFormatter.Serialize(await appMetadataProvider.GetAppMetadata()), context.Request.CallCancelled);
        }
    }
}
