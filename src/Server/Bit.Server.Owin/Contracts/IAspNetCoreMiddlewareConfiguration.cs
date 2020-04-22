using Microsoft.AspNetCore.Builder;

namespace Bit.Owin.Contracts
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
