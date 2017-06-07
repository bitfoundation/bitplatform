using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Contracts.Metadata;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
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
