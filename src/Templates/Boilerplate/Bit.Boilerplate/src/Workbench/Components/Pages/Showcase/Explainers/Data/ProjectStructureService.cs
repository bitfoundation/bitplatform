//+:cnd:noEmit
namespace Boilerplate.Workbench.Components.Pages.Showcase.Explainers.Data;

public partial class ProjectStructureService
{
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    public static IReadOnlyList<ProjectInfo> Projects { get; } =
    [
        new("client-web", "Boilerplate.Client.Web", ProjectKind.Client, nameof(WorkbenchStrings.StructureClientWebRole), nameof(WorkbenchStrings.StructureClientWebDescription), BitIconName.Globe, ["client-core"]),
        new("client-maui", "Boilerplate.Client.Maui", ProjectKind.Client, nameof(WorkbenchStrings.StructureClientMauiRole), nameof(WorkbenchStrings.StructureClientMauiDescription), BitIconName.CellPhone, ["client-core"]),
        new("client-windows", "Boilerplate.Client.Windows", ProjectKind.Client, nameof(WorkbenchStrings.StructureClientWindowsRole), nameof(WorkbenchStrings.StructureClientWindowsDescription), BitIconName.TVMonitor, ["client-core"]),
        new("workbench", "Boilerplate.Workbench", ProjectKind.Tools, nameof(WorkbenchStrings.StructureWorkbenchRole), nameof(WorkbenchStrings.StructureWorkbenchDescription), BitIconName.Design, ["client-core"]),
        new("client-core", "Boilerplate.Client.Core", ProjectKind.Client, nameof(WorkbenchStrings.StructureClientCoreRole), nameof(WorkbenchStrings.StructureClientCoreDescription), BitIconName.Code, ["shared"]),
        new("server-web", "Boilerplate.Server.Web", ProjectKind.Server, nameof(WorkbenchStrings.StructureServerWebRole), nameof(WorkbenchStrings.StructureServerWebDescription), BitIconName.Server, ["client-core", "client-web", "server-api", "server-shared"]),
        new("server-api", "Boilerplate.Server.Api", ProjectKind.Server, nameof(WorkbenchStrings.StructureServerApiRole), nameof(WorkbenchStrings.StructureServerApiDescription), BitIconName.Database, ["shared", "server-shared"]),
        new("shared", "Boilerplate.Shared", ProjectKind.Shared, nameof(WorkbenchStrings.StructureSharedRole), nameof(WorkbenchStrings.StructureSharedDescription), BitIconName.Share, []),
        new("server-shared", "Boilerplate.Server.Shared", ProjectKind.Shared, nameof(WorkbenchStrings.StructureServerSharedRole), nameof(WorkbenchStrings.StructureServerSharedDescription), BitIconName.Settings, []),
        //#if (aspire == true)
        new("apphost", "Boilerplate.Server.AppHost", ProjectKind.Tools, nameof(WorkbenchStrings.StructureAppHostRole), nameof(WorkbenchStrings.StructureAppHostDescription), BitIconName.Rocket, ["server-web", "server-api", "workbench"]),
        //#endif
        new("tests", "Boilerplate.Tests", ProjectKind.Tools, nameof(WorkbenchStrings.StructureTestsRole), nameof(WorkbenchStrings.StructureTestsDescription), BitIconName.TestBeaker, ["server-web"])
    ];

    public Task<IReadOnlyList<ProjectInfo>> GetProjects(CancellationToken cancellationToken) =>
        scenarioState.Load<IReadOnlyList<ProjectInfo>>("structure.projects", () => Projects, () => [], cancellationToken);
}
