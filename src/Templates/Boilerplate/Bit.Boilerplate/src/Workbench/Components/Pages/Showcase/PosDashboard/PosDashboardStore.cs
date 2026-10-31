namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard;

[BlexStore(Name = "pos-dashboard", Persist = true)]
public partial class PosDashboardStore
{
    [BlexState] private PosDashboardFilter _filter = new(PosDashboardDataService.StoreKeys[0], 1);
    [BlexState] private PosWidgetLayout[]? _layout;
    [BlexState] private bool _isEditing;
    [BlexState] private bool _isGalleryOpen;

    [BlexAction] private void OnSetFilter(PosDashboardFilter filter) => Filter = filter;

    [BlexAction] private void OnArrange(PosWidgetLayout[]? layout) => Layout = layout;

    [BlexAction] private void OnEdit(bool isEditing) => IsEditing = isEditing;

    [BlexAction] private void OnOpenGallery(bool isOpen) => IsGalleryOpen = isOpen;
}
