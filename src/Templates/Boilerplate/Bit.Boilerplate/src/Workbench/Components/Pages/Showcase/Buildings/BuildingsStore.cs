namespace Boilerplate.Workbench.Components.Pages.Showcase.Buildings;

[BlexStore(Name = "buildings", Persist = true)]
public partial class BuildingsStore
{
    [BlexState] private Building[]? _items;
    [BlexState] private string? _searchText;
    [BlexState] private Guid? _selectedId;
    [BlexState] private bool _isMapShown;
    [BlexState] private Building? _draft;
    [BlexState] private bool _isDraftNew;
    [BlexState] private double[]? _mapView;

    [BlexAction] private void OnSearch(string? text) => SearchText = text;

    [BlexAction] private void OnSelect(Guid? id) => SelectedId = id;

    [BlexAction] private void OnShowMap(bool isMapShown) => IsMapShown = isMapShown;

    [BlexAction] private void OnStoreItems(Building[] items) => Items = items;

    [BlexAction]
    private void OnEdit(Building draft, bool isNew)
    {
        Draft = draft;
        IsDraftNew = isNew;
    }

    [BlexAction] private void OnCloseEditor() => Draft = null;

    [BlexAction] private void OnMoveMap(double latitude, double longitude, double zoom) => MapView = [latitude, longitude, zoom];
}
