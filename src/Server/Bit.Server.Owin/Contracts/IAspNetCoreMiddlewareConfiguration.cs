using Microsoft.AspNetCore.Builder;

namespace Bit.OwinCore.Contracts
{
    public enum MiddlewarePosition
    {
        BeforeOwinMiddlewares,
        AfterOwinMiddlewares
    }

    public interface IAspNetCoreMiddlewareConfiguration
    {
        void Configure(IApplicationBuilder aspNetCoreApp);

        MiddlewarePosition MiddlewarePosition { get; }
    }
}
