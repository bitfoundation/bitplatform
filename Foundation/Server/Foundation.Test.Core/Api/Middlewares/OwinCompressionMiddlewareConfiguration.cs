using Foundation.Api.Contracts;
using Owin;

namespace Foundation.Test.Api.Middlewares
{
    public class OwinCompressionMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            owinApp.UseCompressionModule();
        }
    }
}
