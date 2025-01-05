//+:cnd:noEmit
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Dashboard;

public partial class DashboardPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Dashboard)];
    protected override string? Subtitle => Localizer[nameof(AppStrings.DashboardSubtitle)];

    [AutoInject] LazyAssemblyLoader lazyAssemblyLoader = default!;

    private bool isLoadingAssemblies = true;
    //#if (signalR == true)
    private Action? unsubscribe;
    //#endif

    protected override async Task OnInitAsync()
    {
        //#if (signalR == true)
        unsubscribe = PubSubService.Subscribe(SharedPubSubMessages.DASHBOARD_DATA_CHANGED, async _ =>
        {
            NavigationManager.NavigateTo(Urls.DashboardPage, replace: true);
        });
        //#endif
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

    //#if (signalR == true)
    protected override ValueTask DisposeAsync(bool disposing)
    {
        unsubscribe?.Invoke();

        return base.DisposeAsync(disposing);
    }
    //#endif
}
