namespace Boilerplate.Client.Core.Components.Pages.Dashboard;

public partial class DashboardPage
{
    private const string StorageKey = "dashboard-widgets";

    private bool isEditing;
    private IList<DashboardWidget> widgets = [];

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        DashboardWidget[] defaults = [new(nameof(OverallStatsWidget), 6), new(nameof(ProductsCountPerCategoryWidget), 3), new(nameof(ProductsPercentageWidget), 3)];

        var saved = InPrerenderSession ? [] : (await StorageService.GetItem(StorageKey))?.Split(',').Select(DashboardWidget.Parse).ToArray() ?? [];

        widgets = [.. saved.Where(s => defaults.Any(d => d.Id == s.Id)), .. defaults.Where(d => saved.Any(s => s.Id == d.Id) is false)];
    }

    private async Task Resize(DashboardWidget widget)
    {
        widget.Span = widget.Span switch { 2 => 3, 3 => 4, 4 => 6, _ => 2 };
        await SaveWidgets();
    }

    private async Task SaveWidgets()
    {
        await StorageService.SetItem(StorageKey, string.Join(',', widgets));
    }

    public class DashboardWidget(string id, int span)
    {
        public string Id { get; } = id;
        public int Span { get; set; } = span;
        public bool Hidden { get; set; }

        public override string ToString() => $"{Id}:{Span}:{Hidden}";

        public static DashboardWidget Parse(string value)
        {
            var parts = value.Split(':');
            return new(parts[0], int.Parse(parts[1])) { Hidden = bool.Parse(parts[2]) };
        }
    }
}
