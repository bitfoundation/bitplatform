namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The flows every Workbench starts with, so the Flows page shows what a flow is before anyone draws one. They live here, not in
/// the review document, so they reach every copy and can't be changed; one whose page was removed from the project is left out.
/// </summary>
public static class WorkbenchSampleFlows
{
    public const string IdPrefix = "sample";
    public const string BuildingsId = $"{IdPrefix}buildings";
    private const string PosId = $"{IdPrefix}pos";

    // The room a phone takes on the board, with space for the labels of the arrows between screens.
    private const double ColumnStep = 850;
    private const double RowStep = 1260;

    // The page state of the screens that need one, by the id the Flows page reads it with: the flow's id, then the screen's.
    private static readonly Dictionary<string, string> states = new()
    {
        [$"{BuildingsId}form"] = """{"buildings":{"Draft":{"id":"d98de887-43b0-49c3-8b78-fd6161c7337d","name":"","city":"","province":"Groningen","type":"Museum","year":2026,"height":null,"floors":null,"latitude":52.2,"longitude":5.3},"IsDraftNew":true}}""",
        [$"{BuildingsId}map"] = """{"buildings":{"IsMapShown":true}}"""
    };

    public static string? StateOf(string id) => states.GetValueOrDefault(id);

    public static IReadOnlyCollection<string> StateIds => states.Keys;

    public static WorkbenchFlow[] Create(IStringLocalizer<WorkbenchStrings> localizer)
    {
        return Create(localizer, pageKey => WorkbenchPageCatalog.Find(pageKey) is not null);
    }

    public static WorkbenchFlow[] Create(IStringLocalizer<WorkbenchStrings> localizer, Func<string, bool> hasPage)
    {
        string Text(string key) => localizer[key];

        WorkbenchFlow[] flows =
        [
            Build(BuildingsId, Text(nameof(WorkbenchStrings.SampleFlowBuildings)),
                  [
                      ("list", "buildings", new(), Text(nameof(WorkbenchStrings.SampleFlowBuildingsList)), 0, RowStep / 2),
                      ("form", "buildings", new(), Text(nameof(WorkbenchStrings.SampleFlowBuildingsForm)), ColumnStep, 0),
                      ("map", "buildings", new(), Text(nameof(WorkbenchStrings.SampleFlowBuildingsMap)), ColumnStep, RowStep)
                  ],
                  [
                      ("list", "form", Text(nameof(WorkbenchStrings.BuildingsAdd))),
                      ("list", "map", Text(nameof(WorkbenchStrings.BuildingsMapView)))
                  ]),
            Build(PosId, Text(nameof(WorkbenchStrings.SampleFlowPos)),
                  [
                      ("loading", "pos-dashboard", new() { Data = WorkbenchDataState.Loading }, Text(nameof(WorkbenchStrings.SampleFlowPosLoading)), 0, RowStep / 2),
                      ("ready", "pos-dashboard", new(), Text(nameof(WorkbenchStrings.SampleFlowPosReady)), ColumnStep, 0),
                      ("mixed", "pos-dashboard", new() { Data = WorkbenchDataState.Mixed }, Text(nameof(WorkbenchStrings.SampleFlowPosMixed)), ColumnStep * 2, 0),
                      ("empty", "pos-dashboard", new() { Data = WorkbenchDataState.Empty }, Text(nameof(WorkbenchStrings.SampleFlowPosEmpty)), ColumnStep, RowStep)
                  ],
                  [
                      ("loading", "ready", Text(nameof(WorkbenchStrings.SampleFlowPosReadyLink))),
                      ("ready", "mixed", Text(nameof(WorkbenchStrings.SampleFlowPosMixedLink))),
                      ("loading", "empty", Text(nameof(WorkbenchStrings.SampleFlowPosEmptyLink)))
                  ])
        ];

        return [.. flows.Where(flow => flow.Screens.All(screen => hasPage(screen.PageKey)))];
    }

    private static WorkbenchFlow Build(string id, string name, (string Id, string PageKey, WorkbenchScenario Scenario, string Note, double X, double Y)[] screens, (string From, string To, string Label)[] links)
    {
        return new(id,
                   name,
                   [.. screens.Select(screen => new WorkbenchFlowScreen(screen.Id, screen.PageKey, screen.Scenario.ToQueryString(), screen.Note, states.ContainsKey($"{id}{screen.Id}"), screen.X, screen.Y))],
                   [.. links.Select(link => new WorkbenchFlowLink($"{link.From}{link.To}", link.From, link.To, link.Label, null))],
                   default);
    }
}
