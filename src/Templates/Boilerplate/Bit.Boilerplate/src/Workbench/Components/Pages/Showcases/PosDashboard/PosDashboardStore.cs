namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

/// <summary>
/// The POS dashboard's state the user controls: the filter, the widget layout, edit mode, the open gallery and the
/// reordered stock. It is persisted per view, so a reload or a screenshot shows the dashboard as it was left.
/// </summary>
[BlexStore(Name = "pos-dashboard", Persist = true)]
public partial class PosDashboardStore
{
    [BlexState] private PosDashboardFilter _filter = PosDashboardFilter.Default;
    // Null while the user keeps the catalog's default layout.
    [BlexState] private PosWidgetLayout[]? _layout;
    [BlexState] private bool _isEditing;
    [BlexState] private bool _isGalleryOpen;
    // The name keys of the stock items reordered from the low-stock widget.
    [BlexState] private string[] _reorderedItems = [];

    [BlexAction] private void OnSetFilter(PosDashboardFilter filter) => Filter = filter;

    [BlexAction] private void OnArrange(PosWidgetLayout[]? layout) => Layout = layout;

    [BlexAction] private void OnEdit(bool isEditing) => IsEditing = isEditing;

    [BlexAction] private void OnOpenGallery(bool isOpen) => IsGalleryOpen = isOpen;

    [BlexAction] private void OnReorder(string itemKey) => ReorderedItems = [.. ReorderedItems, itemKey];
}
