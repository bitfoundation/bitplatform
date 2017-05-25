using Microsoft.AspNetCore.Builder;

namespace Bit.OwinCore.Contracts
{
    public interface IAspNetCoreMiddlewareConfiguration
    {
        void Configure(IApplicationBuilder aspNetCoreApp);

        RegisterKind GetRegisterKind();
    }

    public enum RegisterKind
    {
        BeforeOwinPiepline,
        AfterOwinPipeline
    }
}
