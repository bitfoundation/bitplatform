namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

[BlexStore(Name = "dashboard", Persist = true)]
public partial class DashboardStore
{
    [BlexState] private bool _isEditing;
    [BlexState] private DashboardPage.DashboardWidget[]? _widgets;

    [BlexAction] private void OnEdit(bool isEditing) => IsEditing = isEditing;

    [BlexAction] private void OnArrange(DashboardPage.DashboardWidget[] widgets) => Widgets = widgets;
}
