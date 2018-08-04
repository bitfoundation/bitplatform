using Owin;

namespace Bit.Owin.Contracts
{
    public interface IOwinMiddlewareConfiguration
    {
        void Configure(IAppBuilder owinApp);
    }
}
