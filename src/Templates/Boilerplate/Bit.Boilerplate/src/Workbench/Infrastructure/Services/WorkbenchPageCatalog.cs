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
        new("deployment",
            nameof(WorkbenchStrings.DeployTitle),
            nameof(WorkbenchStrings.DeployDescription),
            BitIconName.Rocket,
            typeof(DeploymentPage),
            nameof(WorkbenchStrings.CategoryExplainers),
            ["bit Blow", "BitToggle", "BitTag", "BitMediaQuery"],
            nameof(WorkbenchStrings.DeployCaption)),
        new("project-structure",
            nameof(WorkbenchStrings.StructureTitle),
            nameof(WorkbenchStrings.StructureDescription),
            BitIconName.Org,
            typeof(ProjectStructurePage),
            nameof(WorkbenchStrings.CategoryExplainers),
            ["bit Blow", "BitMediaQuery", "BitShimmer"],
            nameof(WorkbenchStrings.StructureCaption)),
    ];

    public static WorkbenchPageInfo? Find(string? key) => Pages.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
