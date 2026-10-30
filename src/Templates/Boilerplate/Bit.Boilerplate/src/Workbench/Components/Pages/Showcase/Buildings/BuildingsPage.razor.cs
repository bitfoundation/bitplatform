using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Pages.Showcase.Buildings;

public partial class BuildingsPage
{
    private const string OutlineLayerId = "netherlands-outline";
    private const string MaskLayerId = "netherlands-mask";

    private static readonly BitMapClustering clustering = new();

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
    private BitSnackBar? snackBar;

    private List<Building> buildings = [];
    private List<BitMapMarker> markers = [];
    private IReadOnlyList<Building> selectedItems = [];
    private PosWidgetStatus status = PosWidgetStatus.Loading;
    private string? searchText;
    private Guid? selectedId;
    private bool isNarrow;
    private bool showMap;

    [AutoInject] private BuildingsService buildingsService = default!;
    [AutoInject] private BitMessageBoxService messageBoxService = default!;

    private IEnumerable<Building> FilteredBuildings => string.IsNullOrWhiteSpace(searchText)
        ? buildings
        : buildings.Where(b => b.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                            || b.City.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                            || Localizer[$"Province{b.Province}"].Value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                            || Localizer[$"BuildingType{b.Type}"].Value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await Reload();
    }

    protected override void OnParametersSet()
    {
        BuildMarkers();
    }

    private async Task Reload()
    {
        status = PosWidgetStatus.Loading;
        StateHasChanged();

        try
        {
            buildings = await buildingsService.GetAll(CurrentCancellationToken);
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

    private void BuildMarkers()
    {
        markers = [.. FilteredBuildings.Select(b => new BitMapMarker
        {
            Id = b.Id.ToString(),
            Position = new(b.Latitude, b.Longitude),
            Title = b.Name,
            Alt = b.Name,
            TooltipText = $"{b.Name} · {b.City}",
            Color = b.Id == selectedId ? "#e5484d" : GetTypeColor(b.Type),
            ZIndexOffset = b.Id == selectedId ? 1000 : 0,
            Focusable = true,
            RiseOnHover = true
        })];
    }

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
    }

    private async Task OnMapDoubleClick(BitMapLatLng position)
    {
        if (status is not PosWidgetStatus.Ready and not PosWidgetStatus.Empty)
            return;

        if (NetherlandsBoundary.Contains(position.Latitude, position.Longitude) is false)
        {
            await ShowOutsideWarning();
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

    private async Task Select(Building building, bool flyTo)
    {
        selectedId = building.Id;
        selectedItems = [building];
        BuildMarkers();

        if (flyTo && map is not null)
        {
            await map.FlyTo(new(building.Latitude, building.Longitude), 13);
        }

    }

    private async Task OnMapKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is "Delete" or "Backspace" && buildings.FirstOrDefault(b => b.Id == selectedId) is { } building)
        {
            await ConfirmDelete(building);
        }
    }

    private Task OpenAdd()
    {
        var center = provider.Center;

        return editor!.Open(new Building { Latitude = center.Latitude, Longitude = center.Longitude, Year = DateTime.Now.Year }, isNew: true);
    }

    private Task OpenEdit(Building building) => editor!.Open(building, isNew: false);

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

        status = PosWidgetStatus.Ready;
        await Select(building, flyTo: true);
        await snackBar!.Success(Localizer[nameof(WorkbenchStrings.BuildingsSaved), building.Name]);
    }

    private async Task ConfirmDelete(Building building)
    {
        var confirmed = await messageBoxService.Confirm(Localizer[nameof(WorkbenchStrings.BuildingsDeleteTitle)],
                                                        Localizer[nameof(WorkbenchStrings.BuildingsDeleteBody), building.Name]);

        if (confirmed is false)
            return;

        await buildingsService.Delete(building.Id, CurrentCancellationToken);

        buildings.RemoveAll(b => b.Id == building.Id);
        status = buildings.Count == 0 ? PosWidgetStatus.Empty : PosWidgetStatus.Ready;

        if (selectedId == building.Id)
        {
            selectedId = null;
            selectedItems = [];
        }

        BuildMarkers();
        await snackBar!.Info(Localizer[nameof(WorkbenchStrings.BuildingsDeleted), building.Name]);
    }

    private Task ShowOutsideWarning() => snackBar!.Warning(Localizer[nameof(WorkbenchStrings.BuildingsOutsideTitle)], Localizer[nameof(WorkbenchStrings.BuildingsOutsideBody)]);

    private object? SortByProvince(Building building) => Localizer[$"Province{building.Province}"].Value;

    private object? SortByType(Building building) => Localizer[$"BuildingType{building.Type}"].Value;

    private void OnNarrowChanged(bool narrow)
    {
        isNarrow = narrow;
    }

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
