namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

/// <summary>
/// Hosts the app's dashboard on the view's sample product catalog (See WorkbenchDashboardController). Its widgets load their
/// numbers once the view is interactive, as the app's do, and the layout the page keeps on the device is the view's own
/// (See WorkbenchViewStorage).
/// </summary>
public partial class DashboardHost
{
    private bool isReady;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (IsPrerendering)
            return;

        // The layout the page reads from the device is saved with the view, so it's restored before the page reads it.
        await WhenStateRestored();

        isReady = true;
    }
}
