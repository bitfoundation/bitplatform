using Bit.Bortable;

namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

/// <summary>
/// The point-of-sale dashboard: a grid of widgets the user can rearrange, resize, hide and add back, filtered by store
/// and period. The filter and the layout live in PosDashboardStore, and the page cascades itself to the widgets.
/// </summary>
public partial class PosDashboardPage
{
    /// <summary>The period buttons: how many days each covers and its label.</summary>
    private static readonly (int Days, string TextKey)[] ranges =
    [
        (1, nameof(WorkbenchStrings.PosToday)),
        (7, nameof(WorkbenchStrings.Pos7Days)),
        (30, nameof(WorkbenchStrings.Pos30Days))
    ];

    private Bortable<PosWidgetLayout>? bortable;
    // Working copies of the stored layout: the grid's widgets in order, and those the gallery can add back.
    private IList<PosWidgetLayout> visibleWidgets = [];
    private List<PosWidgetLayout> hiddenWidgets = [];
    private List<BitDropdownItem<string>> storeItems = [];
    private List<BitButtonGroupItem> rangeItems = [];
    // Each widget's DynamicComponent parameters, kept so a render hands it the same dictionary.
    private readonly Dictionary<string, Dictionary<string, object?>> parameters = [];
    // The default layout's cards, shown loading while the saved layout is read.
    private readonly List<PosWidgetLayout> placeholders = PosWidgetCatalog.CreateDefaultLayout();
    // The stored layout the working copies match, so the page tells its own saves from a layout set elsewhere.
    private PosWidgetLayout[]? appliedLayout;
    private bool isLayoutLoaded;
    private bool isSaving;

    [AutoInject] private PosDashboardStore store = default!;

    public PosDashboardFilter Filter => store.Filter;

    public bool IsEditing => store.IsEditing;

    /// <summary>The resource key of the period the widgets' subtitles name.</summary>
    public string PeriodKey => Filter.Days switch
    {
        1 => nameof(WorkbenchStrings.PosToday),
        7 => nameof(WorkbenchStrings.PosLast7Days),
        _ => nameof(WorkbenchStrings.PosLast30Days)
    };

    /// <summary>The resource key of the period a KPI's change is compared with.</summary>
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

        // Prerendering shows the placeholder cards; the saved layout is read once the page is interactive.
        if (IsPrerendering)
            return;

        // The saved layout is read once the store holds what the browser saved.
        await WhenStateRestored();

        // A saved filter outside the offered stores and periods goes back to the default one.
        if (store.Filter is not { } filter || ranges.Any(range => range.Days == filter.Days) is false || PosDashboardDataService.StoreKeys.Contains(filter.StoreKey) is false)
        {
            store.SetFilter(PosDashboardFilter.Default);
        }

        appliedLayout = store.Layout;
        ApplyLayout(LoadLayout());

        isLayoutLoaded = true;

        // A layout the store gets from elsewhere, like a reset, replaces the working copies.
        OwnsSubscription(store.Subscribe(() => store.Layout, layout => OnStoredLayoutChanged(layout)));
    }

    private void SetRange(string? key) => SetFilter(Filter with { Days = int.Parse(key!, CultureInfo.InvariantCulture) });

    /// <summary>Moves a widget one place earlier or later, the widget menu's way to reorder; a move Bortable makes saves through OnReordered.</summary>
    public async Task Move(PosWidgetLayout widget, int delta)
    {
        var index = visibleWidgets.IndexOf(widget);
        var target = Math.Clamp(index + delta, 0, visibleWidgets.Count - 1);

        if (bortable is null || index < 0 || target == index)
            return;

        await bortable.MoveAsync(widget, target);
    }

    public void Resize(PosWidgetLayout widget, PosWidgetSize size)
    {
        widget.Size = size;
        SaveLayout();
    }

    /// <summary>Takes a widget off the grid; the gallery can add it back.</summary>
    public void Hide(PosWidgetLayout widget)
    {
        widget.IsVisible = false;
        visibleWidgets.Remove(widget);
        hiddenWidgets.Add(widget);
        SaveLayout();
    }

    /// <summary>Adds a widget from the gallery to the end of the grid, and closes the gallery once nothing is left in it.</summary>
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

    /// <summary>Keeps and saves the order a drag left.</summary>
    private void OnReordered(IList<PosWidgetLayout> widgets)
    {
        visibleWidgets = widgets;
        SaveLayout();
    }

    private void SetFilter(PosDashboardFilter filter) => store.SetFilter(filter);

    /// <summary>Turns edit mode on, or off after a short simulated save.</summary>
    private async Task ToggleEditing()
    {
        if (IsEditing is false)
        {
            store.Edit(true);
            return;
        }

        isSaving = true;

        await ScenarioState.Save();

        isSaving = false;
        store.Edit(false);
    }

    /// <summary>Goes back to the catalog's default layout and forgets the saved one.</summary>
    private void ResetLayout()
    {
        ApplyLayout(PosWidgetCatalog.CreateDefaultLayout());
        appliedLayout = null;
        store.Arrange(null);
    }

    /// <summary>Stores the layout, shown widgets first, as copies, so the store never holds objects the page changes.</summary>
    private void SaveLayout()
    {
        appliedLayout = [.. visibleWidgets.Concat(hiddenWidgets).Select(w => new PosWidgetLayout { Key = w.Key, Size = w.Size, IsVisible = w.IsVisible })];
        store.Arrange(appliedLayout);
    }

    /// <summary>Takes a layout the store got from elsewhere, like a reset or a restored snapshot; the page's own saves are skipped.</summary>
    private void OnStoredLayoutChanged(PosWidgetLayout[]? layout)
    {
        if (ReferenceEquals(layout, appliedLayout))
            return;

        appliedLayout = layout;

        _ = InvokeAsync(() =>
        {
            ApplyLayout(LoadLayout());
            StateHasChanged();
        });
    }

    /// <summary>The saved layout without unknown or repeated widgets, plus catalog widgets it lacks; the default when none is saved.</summary>
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

    /// <summary>Splits a layout into the grid's widgets and the gallery's.</summary>
    private void ApplyLayout(List<PosWidgetLayout> layout)
    {
        visibleWidgets = [.. layout.Where(w => w.IsVisible)];
        hiddenWidgets = [.. layout.Where(w => w.IsVisible is false)];
    }

    /// <summary>The grid item's class, which sets how many columns it spans.</summary>
    private static string GetItemClass(PosWidgetLayout widget) => $"widget-{widget.Size.ToString().ToLowerInvariant()}";

    /// <summary>The widget's name, which Bortable announces while it is moved.</summary>
    private string GetItemLabel(PosWidgetLayout widget) => Localizer[PosWidgetCatalog.Find(widget.Key)!.TitleKey];

    /// <summary>The parameters a widget's DynamicComponent gets, reused until another layout object takes the widget's place.</summary>
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
