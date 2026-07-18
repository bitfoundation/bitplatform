//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class DashboardPage
{
    //#if (signalR == true)
    private Action? unsubscribe;
    //#endif

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        //#if (signalR == true)
        unsubscribe = PubSubService.Subscribe(SharedAppMessages.DASHBOARD_DATA_CHANGED, async _ =>
        {
            NavigationManager.RefreshCurrentPage();
        });
        //#endif
    }

    //#if (signalR == true)
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribe?.Invoke();
    }
    //#endif
}
