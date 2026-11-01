namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

/// <summary>
/// The Workbench copy of the app's dashboard. The widget layout and the edit mode live in <c>DashboardStore</c>, where
/// the app keeps the layout in its storage service and the edit mode in a field.
/// </summary>
public partial class DashboardPage
{
    private static readonly DashboardWidget[] defaults = [new(nameof(OverallStatsWidget), 6), new(nameof(ProductsCountPerCategoryWidget), 3), new(nameof(ProductsPercentageWidget), 3)];

    [AutoInject] private DashboardStore store = default!;

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        await WhenStateRestored();
    }

    /// <summary>The saved layout without widgets that no longer exist, then the missing ones with their defaults.</summary>
    private IList<DashboardWidget> Widgets
    {
        get
        {
            var saved = store.Widgets ?? [];

            return [.. saved.Where(s => defaults.Any(d => d.Id == s.Id)), .. defaults.Where(d => saved.Any(s => s.Id == d.Id) is false)];
        }
    }

    private void Resize(DashboardWidget widget)
    {
        SaveWidgets([.. Widgets.Select(w => w.Id == widget.Id ? w with { Span = w.Span switch { 2 => 3, 3 => 4, 4 => 6, _ => 2 } } : w)]);
    }

    private void Hide(DashboardWidget widget, bool hidden)
    {
        SaveWidgets([.. Widgets.Select(w => w.Id == widget.Id ? w with { Hidden = hidden } : w)]);
    }

    private void SaveWidgets(IList<DashboardWidget> widgets) => store.Arrange([.. widgets]);

    /// <summary>A widget's place in the layout: its id, its span out of six columns and whether it is hidden.</summary>
    public record DashboardWidget(string Id, int Span, bool Hidden = false)
    {
        /// <summary>
        /// Equal by id, as the app's widget is the same instance after a change: a resized or hidden widget keeps its
        /// element, its loaded data and the focus.
        /// </summary>
        public virtual bool Equals(DashboardWidget? other) => other is not null && other.Id == Id;

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);
    }
}
