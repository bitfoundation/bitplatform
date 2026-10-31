namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

public partial class DashboardPage
{
    private static readonly DashboardWidget[] defaults = [new(nameof(OverallStatsWidget), 6), new(nameof(ProductsCountPerCategoryWidget), 3), new(nameof(ProductsPercentageWidget), 3)];

    [AutoInject] private DashboardStore store = default!;

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        await WhenStateRestored();
    }

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

    public record DashboardWidget(string Id, int Span, bool Hidden = false);
}
