using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace AdminPanel.Client.Core.Components.Pages.Authorized.Dashboard;

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
                    "System.Private.Xml.wasm", "System.Data.Common.wasm",
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
