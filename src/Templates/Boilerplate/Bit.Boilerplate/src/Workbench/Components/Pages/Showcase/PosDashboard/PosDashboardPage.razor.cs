using Bit.Bortable;

namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard;

public partial class PosDashboardPage
{
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
    private bool isSaving;

    [AutoInject] private PosDashboardStore store = default!;

    public PosDashboardFilter Filter => store.Filter;

    public bool IsEditing => store.IsEditing;

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

        Subscribe(store);

        await WhenStateRestored();

        ApplyLayout(LoadLayout());

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
        SaveLayout();
    }

    public void Resize(PosWidgetLayout widget, PosWidgetSize size)
    {
        widget.Size = size;
        SaveLayout();
    }

    public void Hide(PosWidgetLayout widget)
    {
        widget.IsVisible = false;
        visibleWidgets.Remove(widget);
        hiddenWidgets.Add(widget);
        SaveLayout();
    }

    private void Show(PosWidgetLayout widget)
    {
        widget.IsVisible = true;
        hiddenWidgets.Remove(widget);
        visibleWidgets.Add(widget);
        SaveLayout();

        if (hiddenWidgets.Count == 0)
        {
            store.OpenGallery(false);
        }
    }

    private void OnReordered(IList<PosWidgetLayout> widgets)
    {
        visibleWidgets = widgets;
        SaveLayout();
    }

    private void SetFilter(PosDashboardFilter filter) => store.SetFilter(filter);

    private async Task ToggleEditing()
    {
        if (IsEditing is false)
        {
            store.Edit(true);
            return;
        }

        isSaving = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);
        }
        finally
        {
            isSaving = false;
            store.Edit(false);
        }
    }

    private void ResetLayout()
    {
        ApplyLayout(PosWidgetCatalog.CreateDefaultLayout());
        store.Arrange(null);
    }

    private void SaveLayout() => store.Arrange([.. visibleWidgets.Concat(hiddenWidgets).Select(w => new PosWidgetLayout { Key = w.Key, Size = w.Size, IsVisible = w.IsVisible })]);

    private List<PosWidgetLayout> LoadLayout()
    {
        if (store.Layout is not { Length: > 0 } saved)
            return PosWidgetCatalog.CreateDefaultLayout();

        var known = saved.Where(w => PosWidgetCatalog.Find(w.Key) is not null)
                         .DistinctBy(w => w.Key)
                         .Select(w => new PosWidgetLayout { Key = w.Key, Size = w.Size, IsVisible = w.IsVisible })
                         .ToList();

        known.AddRange(PosWidgetCatalog.CreateDefaultLayout().Where(d => known.Any(w => w.Key == d.Key) is false));

        return known;
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
