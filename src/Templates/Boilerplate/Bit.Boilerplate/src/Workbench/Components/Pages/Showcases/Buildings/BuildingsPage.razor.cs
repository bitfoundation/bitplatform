using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Pages.Showcases.Buildings;

/// <summary>
/// A data grid (or cards) and a map of the Netherlands that stay in sync: selecting a building on either selects it on
/// both. Users the ManageBuildings policy allows add, edit and delete; their changes live in <c>BuildingsStore</c>.
/// </summary>
public partial class BuildingsPage
{
    private const string OutlineLayerId = "netherlands-outline";
    private const string MaskLayerId = "netherlands-mask";

    private static readonly BitMapClustering clustering = new();

    // The Dutch government's base map, which can't be panned or zoomed out of the country.
    private readonly BitLeafletMapProvider provider = new()
    {
        TileUrl = "https://service.pdok.nl/brt/achtergrondkaart/wmts/v2_0/standaard/EPSG:3857/{z}/{x}/{y}.png",
        TileAttribution = "Kaartgegevens &copy; <a href=\"https://www.kadaster.nl\">Kadaster</a>",
        TileMaxZoom = 19,
        Center = new(52.2, 5.3),
        Zoom = 7,
        MinZoom = 7,
        DoubleClickZoom = false,
        MaxBounds = new(new(50.4, 2.6), new(53.9, 7.6))
    };

    private BitMap<BitLeafletMapProvider>? map;
    private BitDataGrid<Building>? grid;
    private BuildingEditor? editor;

    private List<Building> buildings = [];
    private List<BitMapMarker> markers = [];
    private IReadOnlyList<Building> selectedItems = [];
    private bool canManage;
    private PosWidgetStatus status = PosWidgetStatus.Loading;
    private bool isRestored;
    private bool isNarrow;

    [AutoInject] private BuildingsStore store = default!;
    [AutoInject] private BuildingsService buildingsService = default!;
    [AutoInject] private BitMessageBoxService messageBoxService = default!;

    private string? SearchText => store.SearchText;

    /// <summary>The loaded buildings whose name, city, province or type, as shown, contains the search text.</summary>
    private IEnumerable<Building> FilteredBuildings => string.IsNullOrWhiteSpace(SearchText)
        ? buildings
        : buildings.Where(b => b.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                            || b.City.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                            || Localizer[$"Province{b.Province}"].Value.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                            || Localizer[$"BuildingType{b.Type}"].Value.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

    protected override async Task OnParametersSetAsync()
    {
        // Whether the signed-in user may add, edit and delete buildings.
        canManage = await AuthorizationService.IsAuthorized((await AuthenticationStateTask).User, WorkbenchPolicies.ManageBuildings);
    }

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();

        // The editor reopens the draft the store holds, so it renders only once the store is restored.
        isRestored = true;

        await Reload();
    }

    protected override void OnParametersSet()
    {
        BuildMarkers();
    }

    /// <summary>Loads the buildings through the scenario state and selects the one the store remembers.</summary>
    private async Task Reload()
    {
        status = PosWidgetStatus.Loading;
        StateHasChanged();

        try
        {
            buildings = await buildingsService.GetAll(CurrentCancellationToken);
            selectedItems = [.. buildings.Where(b => b.Id == store.SelectedId)];
            status = buildings.Count == 0 ? PosWidgetStatus.Empty : PosWidgetStatus.Ready;
        }
        catch (WorkbenchSimulatedException)
        {
            status = PosWidgetStatus.Failed;
        }
        catch (OperationCanceledException)
        {
        }

        BuildMarkers();
    }

    /// <summary>Rebuilds the markers from the filtered buildings, with the selected one red and on top.</summary>
    private void BuildMarkers()
    {
        markers = [.. FilteredBuildings.Select(b => new BitMapMarker
        {
            Id = b.Id.ToString(),
            Position = new(b.Latitude, b.Longitude),
            Title = b.Name,
            Alt = b.Name,
            TooltipText = $"{b.Name} · {b.City}",
            Color = b.Id == store.SelectedId ? "#e5484d" : GetTypeColor(b.Type),
            ZIndexOffset = b.Id == store.SelectedId ? 1000 : 0,
            Focusable = true,
            RiseOnHover = true
        })];
    }

    /// <summary>Draws the country's outline, dims everything outside it and goes back to the saved view.</summary>
    private async Task OnMapReady()
    {
        if (map is null)
            return;

        await map.AddGeoJson(MaskLayerId, NetherlandsBoundary.BuildOutsideMask(), new BitMapVectorPathStyle
        {
            Color = "transparent",
            Fill = true,
            FillColor = "rgba(15, 23, 42, 0.28)"
        });

        await map.AddGeoJson(OutlineLayerId, NetherlandsBoundary.GeoJson, new BitMapVectorPathStyle
        {
            Color = "#4f6bed",
            Fill = false,
            DashArray = "6 4"
        });

        if (store.MapView is [var latitude, var longitude, var zoom])
        {
            await map.SetView(new(latitude, longitude), zoom, animate: false);
        }
    }

    private void OnMapViewChanged(BitMapViewState view)
    {
        store.MoveMap(view.Center.Latitude, view.Center.Longitude, view.Zoom);
    }

    private async Task Search(string? text)
    {
        store.Search(text);
        BuildMarkers();
        await RefreshGrid();
    }

    /// <summary>The grid's next batch of the filtered buildings, filtered and sorted the way its filter row and headers ask.</summary>
    private Task<BitDataGridReadResult<Building>> LoadMore(BitDataGridReadRequest request)
    {
        var rows = FilteredBuildings;

        foreach (var filter in request.Filters)
        {
            rows = (filter.ColumnId, filter.Value) switch
            {
                (nameof(Building.Name), string name) => rows.Where(b => b.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase)),
                (nameof(Building.Type), BuildingType type) => rows.Where(b => b.Type == type),
                (nameof(Building.Year), int year) => rows.Where(b => b.Year == year),
                _ => rows
            };
        }

        IOrderedEnumerable<Building>? sorted = null;

        foreach (var sort in request.Sorts.Where(s => s.Direction is not BitDataGridSortDirection.None).OrderBy(s => s.Priority))
        {
            Func<Building, object?> key = sort.ColumnId switch
            {
                nameof(Building.Type) => SortByType,
                nameof(Building.Year) => b => b.Year,
                _ => b => b.Name
            };

            var descending = sort.Direction is BitDataGridSortDirection.Descending;

            sorted = sorted is null
                ? (descending ? rows.OrderByDescending(key) : rows.OrderBy(key))
                : (descending ? sorted.ThenByDescending(key) : sorted.ThenBy(key));
        }

        // Take is null when an export asks for every row.
        var batch = (sorted ?? rows).Skip(request.Skip).Take(request.Take ?? int.MaxValue).ToList();

        return Task.FromResult(new BitDataGridReadResult<Building>(batch, 0));
    }

    /// <summary>Reloads the grid from its first batch. Only a grid already on screen needs it; one that appears loads its own.</summary>
    private Task RefreshGrid() => status is PosWidgetStatus.Ready && Scenario.Variant is not "cards" && grid is not null
        ? grid.RefreshAsync()
        : Task.CompletedTask;

    /// <summary>Opens the editor for a new building where the map was double-clicked, if that is inside the country.</summary>
    private async Task OnMapDoubleClick(BitMapLatLng position)
    {
        if (canManage is false || status is not PosWidgetStatus.Ready and not PosWidgetStatus.Empty)
            return;

        if (NetherlandsBoundary.Contains(position.Latitude, position.Longitude) is false)
        {
            ShowOutsideWarning();
            return;
        }

        await editor!.Open(new Building
        {
            Latitude = Math.Round(position.Latitude, 5),
            Longitude = Math.Round(position.Longitude, 5),
            Year = DateTime.Now.Year
        }, isNew: true);
    }

    private async Task OnMarkerClick(string markerId)
    {
        var building = buildings.FirstOrDefault(b => b.Id.ToString() == markerId);

        if (building is null)
            return;

        await Select(building, flyTo: false);
    }

    private async Task OnMarkerDoubleClick(string markerId)
    {
        var building = buildings.FirstOrDefault(b => b.Id.ToString() == markerId);

        if (building is not null)
        {
            await OpenEdit(building);
        }
    }

    private async Task OnGridSelectionChanged(IReadOnlyList<Building> items)
    {
        selectedItems = items;

        if (items.FirstOrDefault() is { } building)
        {
            await Select(building, flyTo: true);
        }
    }

    /// <summary>Selects the building everywhere; <paramref name="flyTo"/> also moves the map to it when it is visible.</summary>
    private async Task Select(Building building, bool flyTo)
    {
        store.Select(building.Id);
        selectedItems = [building];
        BuildMarkers();

        if (flyTo && map is not null && (isNarrow is false || store.IsMapShown))
        {
            await map.FlyTo(new(building.Latitude, building.Longitude), 13);
        }

    }

    /// <summary>Delete or Backspace on the map deletes the selected building, after a confirmation.</summary>
    private async Task OnMapKeyDown(KeyboardEventArgs args)
    {
        if (canManage && args.Key is "Delete" or "Backspace" && buildings.FirstOrDefault(b => b.Id == store.SelectedId) is { } building)
        {
            await ConfirmDelete(building);
        }
    }

    /// <summary>Opens the editor for a new building at the map's starting center.</summary>
    private Task OpenAdd()
    {
        var center = provider.Center;

        return editor!.Open(new Building { Latitude = center.Latitude, Longitude = center.Longitude, Year = DateTime.Now.Year }, isNew: true);
    }

    private Task OpenEdit(Building building) => canManage ? editor!.Open(building, isNew: false) : Task.CompletedTask;

    /// <summary>Puts the saved building into the list, or adds it, and selects it.</summary>
    private async Task OnSaved(Building building)
    {
        var index = buildings.FindIndex(b => b.Id == building.Id);

        if (index >= 0)
        {
            buildings[index] = building;
        }
        else
        {
            buildings.Add(building);
        }

        await RefreshGrid();
        status = PosWidgetStatus.Ready;
        await Select(building, flyTo: true);
        SnackBarService.Success(Localizer[nameof(WorkbenchStrings.BuildingsSaved), building.Name]);
    }

    private async Task ConfirmDelete(Building building)
    {
        var confirmed = await messageBoxService.Confirm(Localizer[nameof(WorkbenchStrings.BuildingsDeleteTitle)],
                                                        Localizer[nameof(WorkbenchStrings.BuildingsDeleteBody), building.Name]);

        if (confirmed is false)
            return;

        try
        {
            await buildingsService.Delete(building.Id, CurrentCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        buildings.RemoveAll(b => b.Id == building.Id);
        await RefreshGrid();
        status = buildings.Count == 0 ? PosWidgetStatus.Empty : PosWidgetStatus.Ready;

        if (store.SelectedId == building.Id)
        {
            store.Select(null);
            selectedItems = [];
        }

        BuildMarkers();
        SnackBarService.Info(Localizer[nameof(WorkbenchStrings.BuildingsDeleted), building.Name]);
    }

    private void ShowOutsideWarning() => SnackBarService.Warning(Localizer[nameof(WorkbenchStrings.BuildingsOutsideTitle)], Localizer[nameof(WorkbenchStrings.BuildingsOutsideBody)]);

    /// <summary>Sorts by the type's translated name, the way the column shows it.</summary>
    private object? SortByType(Building building) => Localizer[$"BuildingType{building.Type}"].Value;

    private void OnNarrowChanged(bool narrow)
    {
        isNarrow = narrow;
    }

    /// <summary>The color that marks a building type in the list and on the map, a data color the theme check leaves alone.</summary>
    public static string GetTypeColor(BuildingType type) => type switch
    {
        BuildingType.Museum => "#8b5cf6",
        BuildingType.Church => "#4f6bed",
        BuildingType.Tower => "#0fb5ae",
        BuildingType.Station => "#f5a524",
        BuildingType.Stadium => "#22a06b",
        BuildingType.Palace => "#e5487d",
        BuildingType.Office => "#64748b",
        BuildingType.CityHall => "#d97706",
        _ => "#0ea5e9"
    };
}
