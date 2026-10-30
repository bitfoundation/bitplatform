using Bit.Bortable;

namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard;

public partial class PosDashboardPage
{
    private const string LayoutStorageKey = "workbench.pos-dashboard.layout";

    private static readonly (int Days, string TextKey)[] ranges =
    [
        (1, nameof(WorkbenchStrings.PosToday)),
        (7, nameof(WorkbenchStrings.Pos7Days)),
        (30, nameof(WorkbenchStrings.Pos30Days))
    ];

    private Bortable<PosWidgetLayout>? bortable;
    private IList<PosWidgetLayout> visibleWidgets = [];
    private List<PosWidgetLayout> hiddenWidgets = [];
    private List<BitDropdownItem<string>> storeItems = [];
    private List<BitButtonGroupItem> rangeItems = [];
    private readonly Dictionary<string, Dictionary<string, object?>> parameters = [];
    private bool isLayoutLoaded;
    private bool isGalleryOpen;
    private bool isSaving;

    [AutoInject] private IStorageService storageService = default!;

    public PosDashboardFilter Filter { get; private set; } = new(PosDashboardDataService.StoreKeys[0], 1);

    public bool IsEditing { get; private set; }

    public string PeriodKey => Filter.Days switch
    {
        1 => nameof(WorkbenchStrings.PosToday),
        7 => nameof(WorkbenchStrings.PosLast7Days),
        _ => nameof(WorkbenchStrings.PosLast30Days)
    };

    public string ComparisonKey => Filter.Days switch
    {
        1 => nameof(WorkbenchStrings.PosVsYesterday),
        7 => nameof(WorkbenchStrings.PosVsPrevious7Days),
        _ => nameof(WorkbenchStrings.PosVsPrevious30Days)
    };

    protected override async Task OnInitializedAsync()
    {
        storeItems = [.. PosDashboardDataService.StoreKeys.Select(key => new BitDropdownItem<string> { Value = key, Text = Localizer[key] })];
        rangeItems = [.. ranges.Select(range => new BitButtonGroupItem { Key = range.Days.ToString(CultureInfo.InvariantCulture), Text = Localizer[range.TextKey] })];

        ApplyLayout(await ScenarioState.Track(LoadLayout()));

        isLayoutLoaded = true;
    }

    private void SetRange(BitButtonGroupItem item) => SetFilter(Filter with { Days = int.Parse(item.Key!, CultureInfo.InvariantCulture) });

    public async Task Move(PosWidgetLayout widget, int delta)
    {
        var index = visibleWidgets.IndexOf(widget);
        var target = Math.Clamp(index + delta, 0, visibleWidgets.Count - 1);

        if (bortable is null || index < 0 || target == index)
            return;

        await bortable.MoveAsync(widget, target);
        StateHasChanged();
    }

    public void Resize(PosWidgetLayout widget, PosWidgetSize size)
    {
        widget.Size = size;
        StateHasChanged();
    }

    public void Hide(PosWidgetLayout widget)
    {
        widget.IsVisible = false;
        visibleWidgets.Remove(widget);
        hiddenWidgets.Add(widget);
        StateHasChanged();
    }

    private void Show(PosWidgetLayout widget)
    {
        widget.IsVisible = true;
        hiddenWidgets.Remove(widget);
        visibleWidgets.Add(widget);

        if (hiddenWidgets.Count == 0)
        {
            isGalleryOpen = false;
        }
    }

    private void SetFilter(PosDashboardFilter filter)
    {
        Filter = filter;
    }

    private async Task ToggleEditing()
    {
        if (IsEditing is false)
        {
            IsEditing = true;
            return;
        }

        isSaving = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);
            await storageService.SetItem(LayoutStorageKey, JsonSerializer.Serialize([.. visibleWidgets, .. hiddenWidgets], PosDashboardJsonContext.Default.ListPosWidgetLayout));
        }
        finally
        {
            isSaving = false;
            IsEditing = false;
        }
    }

    private async Task ResetLayout()
    {
        ApplyLayout(PosWidgetCatalog.CreateDefaultLayout());
        await storageService.RemoveItem(LayoutStorageKey);
    }

    private async Task<List<PosWidgetLayout>> LoadLayout()
    {
        try
        {
            var json = await storageService.GetItem(LayoutStorageKey);

            if (string.IsNullOrWhiteSpace(json) is false)
            {
                var saved = JsonSerializer.Deserialize(json, PosDashboardJsonContext.Default.ListPosWidgetLayout) ?? [];
                var known = saved.Where(w => PosWidgetCatalog.Find(w.Key) is not null).DistinctBy(w => w.Key).ToList();
                known.AddRange(PosWidgetCatalog.CreateDefaultLayout().Where(d => known.Any(w => w.Key == d.Key) is false));
                return known;
            }
        }
        catch (JsonException)
        {
        }

        return PosWidgetCatalog.CreateDefaultLayout();
    }

    private void ApplyLayout(List<PosWidgetLayout> layout)
    {
        visibleWidgets = [.. layout.Where(w => w.IsVisible)];
        hiddenWidgets = [.. layout.Where(w => w.IsVisible is false)];
    }

    private static string GetItemClass(PosWidgetLayout widget) => $"widget-{widget.Size.ToString().ToLowerInvariant()}";

    private string GetItemLabel(PosWidgetLayout widget) => Localizer[PosWidgetCatalog.Find(widget.Key)!.TitleKey];

    private Dictionary<string, object?> GetParameters(PosWidgetLayout widget)
    {
        var definition = PosWidgetCatalog.Find(widget.Key)!;

        if (parameters.TryGetValue(widget.Key, out var cached) && ReferenceEquals(cached[nameof(PosWidgetBase<object>.Layout)], widget))
            return cached;

        var created = new Dictionary<string, object?>(definition.Parameters ?? new Dictionary<string, object?>())
        {
            [nameof(PosWidgetBase<object>.Layout)] = widget,
            [nameof(PosWidgetBase<object>.Definition)] = definition
        };

        parameters[widget.Key] = created;

        return created;
    }
}
