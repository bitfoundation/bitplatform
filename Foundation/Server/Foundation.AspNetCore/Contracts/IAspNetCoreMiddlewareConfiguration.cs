using Microsoft.AspNetCore.Builder;

namespace Foundation.AspNetCore.Contracts
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
