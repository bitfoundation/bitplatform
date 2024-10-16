//+:cnd:noEmit
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.Client.Core.Components.Pages.Main.Dashboard;

[Authorize]
public partial class DashboardPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Dashboard)];
    protected override string? Subtitle => Localizer[nameof(AppStrings.DashboardSubtitle)];

    [AutoInject] LazyAssemblyLoader lazyAssemblyLoader = default!;

    private bool isLoadingAssemblies = true;

    protected async override Task OnInitAsync()
    {
        try
        {
            if (AppPlatform.IsBrowser)
            {
                await lazyAssemblyLoader.LoadAssembliesAsync([
                    //#if (offlineDb == false)
                    "System.Private.Xml.wasm", "System.Data.Common.wasm",
                    //#endif
                    "Newtonsoft.Json.wasm"]
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
