using Owin;

namespace Foundation.Api.Contracts
{
    public interface IOwinMiddlewareConfiguration
    {
        void Configure(IAppBuilder owinApp);
    }
}