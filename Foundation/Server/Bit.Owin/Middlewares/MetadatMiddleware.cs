using System.Threading.Tasks;
using Foundation.Api.Contracts.Metadata;
using Foundation.Core.Contracts;
using Microsoft.Owin;

namespace Foundation.Api.Middlewares
{
    public class MetadatMiddleware : OwinMiddleware
    {
        public MetadatMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            IContentFormatter contentFormatter =
                context.GetDependencyResolver().Resolve<IContentFormatter>();

            IAppMetadataProvider appMetadataProvider =
                context.GetDependencyResolver().Resolve<IAppMetadataProvider>();

            context.Response.ContentType = "application/json; charset=utf-8";

            await context.Response.WriteAsync(contentFormatter.Serialize(await appMetadataProvider.GetAppMetadata()), context.Request.CallCancelled);
        }
    }
}
