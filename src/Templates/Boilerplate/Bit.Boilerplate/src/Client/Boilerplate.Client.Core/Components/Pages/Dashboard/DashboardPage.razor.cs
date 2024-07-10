//+:cnd:noEmit
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

[Authorize]
public partial class DashboardPage
{
    [AutoInject] LazyAssemblyLoader lazyAssemblyLoader = default!;

    private bool isLoadingAssemblies = true;

    protected async override Task OnInitAsync()
    {
        try
        {
            if (AppOperatingSystem.IsBrowser)
            {
                await lazyAssemblyLoader.LoadAssembliesAsync([
                    //#if (sample == "Admin" && offlineDb == false)
                    "System.Private.Xml.wasm", "System.Data.Common.wasm",
                    //#endif
                    //#if (sample == "Admin")
                    "Newtonsoft.Json.wasm"]
                    //#endif
                    );
            }
        }
        finally
        {
            isLoadingAssemblies = false;
        }

        await base.OnInitAsync();
    }
}
