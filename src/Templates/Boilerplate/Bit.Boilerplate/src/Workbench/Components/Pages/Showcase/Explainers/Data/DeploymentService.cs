namespace Boilerplate.Workbench.Components.Pages.Showcase.Explainers.Data;

public partial class DeploymentService
{
    public const string FailingStepKey = "check";

    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    public static IReadOnlyList<DeploymentStep> Steps { get; } =
    [
        new("merge", 0, nameof(WorkbenchStrings.DeployMergeTitle), nameof(WorkbenchStrings.DeployMergeDetail), nameof(WorkbenchStrings.DeployMergeDescription), BitIconName.BranchMerge, ["images", "windows", "android", "ios"]),
        new("images", 1, nameof(WorkbenchStrings.DeployImagesTitle), nameof(WorkbenchStrings.DeployImagesDetail), nameof(WorkbenchStrings.DeployImagesDescription), BitIconName.Package, ["compose"]),
        new("windows", 1, nameof(WorkbenchStrings.DeployWindowsTitle), nameof(WorkbenchStrings.DeployWindowsDetail), nameof(WorkbenchStrings.DeployWindowsDescription), BitIconName.TVMonitor, []),
        new("android", 1, nameof(WorkbenchStrings.DeployAndroidTitle), nameof(WorkbenchStrings.DeployAndroidDetail), nameof(WorkbenchStrings.DeployAndroidDescription), BitIconName.CellPhone, []),
        new("ios", 1, nameof(WorkbenchStrings.DeployIosTitle), nameof(WorkbenchStrings.DeployIosDetail), nameof(WorkbenchStrings.DeployIosDescription), BitIconName.CellPhone, []),
        new("compose", 2, nameof(WorkbenchStrings.DeployComposeTitle), nameof(WorkbenchStrings.DeployComposeDetail), nameof(WorkbenchStrings.DeployComposeDescription), BitIconName.Documentation, ["pull"]),
        new("pull", 3, nameof(WorkbenchStrings.DeployPullTitle), nameof(WorkbenchStrings.DeployPullDetail), nameof(WorkbenchStrings.DeployPullDescription), BitIconName.CloudDownload, ["migrate"]),
        new("migrate", 4, nameof(WorkbenchStrings.DeployMigrateTitle), nameof(WorkbenchStrings.DeployMigrateDetail), nameof(WorkbenchStrings.DeployMigrateDescription), BitIconName.Database, ["start"]),
        new("start", 5, nameof(WorkbenchStrings.DeployStartTitle), nameof(WorkbenchStrings.DeployStartDetail), nameof(WorkbenchStrings.DeployStartDescription), BitIconName.Play, [FailingStepKey]),
        new(FailingStepKey, 6, nameof(WorkbenchStrings.DeployCheckTitle), nameof(WorkbenchStrings.DeployCheckDetail), nameof(WorkbenchStrings.DeployCheckDescription), BitIconName.Heart, ["purge"]),
        new("purge", 7, nameof(WorkbenchStrings.DeployPurgeTitle), nameof(WorkbenchStrings.DeployPurgeDetail), nameof(WorkbenchStrings.DeployPurgeDescription), BitIconName.Cloud, ["live"]),
        new("live", 8, nameof(WorkbenchStrings.DeployLiveTitle), nameof(WorkbenchStrings.DeployLiveDetail), nameof(WorkbenchStrings.DeployLiveDescription), BitIconName.Globe, [])
    ];

    public Task<bool> HasLastRun(CancellationToken cancellationToken) =>
        scenarioState.Load("deployment.last-run", () => true, () => false, cancellationToken);

    public Task RunStep(DeploymentStep step, CancellationToken cancellationToken)
    {
        var isSlow = scenarioState.Scenario.Data is WorkbenchDataState.Slow;

        return Task.Delay(isSlow ? Random.Shared.Next(1600, 2600) : Random.Shared.Next(650, 1150), cancellationToken);
    }
}
