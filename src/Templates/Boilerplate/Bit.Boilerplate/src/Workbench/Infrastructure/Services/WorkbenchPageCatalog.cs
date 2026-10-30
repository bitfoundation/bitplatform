using Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard;

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchPageCatalog
{
    public static IReadOnlyList<WorkbenchPageInfo> Pages { get; } =
    [
        new("pos-dashboard",
            nameof(WorkbenchStrings.PosDashboardTitle),
            nameof(WorkbenchStrings.PosDashboardDescription),
            BitIconName.BarChartVerticalFill,
            typeof(PosDashboardPage),
            nameof(WorkbenchStrings.CategoryDashboards),
            ["bit Bortable", "BitChart", "BitShimmer", "bit Bmotion"]),
    ];

    public static WorkbenchPageInfo? Find(string? key) => Pages.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
