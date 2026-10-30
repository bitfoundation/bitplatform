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
            ["bit Bortable", "BitChart", "BitShimmer", "bit Bmotion"],
            nameof(WorkbenchStrings.PosDashboardCaption)),
        new("buildings",
            nameof(WorkbenchStrings.BuildingsTitle),
            nameof(WorkbenchStrings.BuildingsDescription),
            BitIconName.MapPin,
            typeof(BuildingsPage),
            nameof(WorkbenchStrings.CategoryDataAndMaps),
            ["BitDataGrid", "BitMap", "BitModal", "BitMessageBox", "BitSnackBar"],
            nameof(WorkbenchStrings.BuildingsCaption)),
        new("assistant",
            nameof(WorkbenchStrings.AssistantTitle),
            nameof(WorkbenchStrings.AssistantDescription),
            BitIconName.Robot,
            typeof(AssistantPage),
            nameof(WorkbenchStrings.CategoryAi),
            ["BitMarkdownViewer", "BitTextShimmer", "BitChart", "BitPanel", "bit Bmotion"],
            nameof(WorkbenchStrings.AssistantCaption)),
    ];

    public static WorkbenchPageInfo? Find(string? key) => Pages.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
