using Microsoft.AspNetCore.Builder;

namespace Bit.OwinCore.Contracts
{
    public interface IAspNetCoreMiddlewareConfiguration
    {
        void Configure(IApplicationBuilder aspNetCoreApp);
    }
}
