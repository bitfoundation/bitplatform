namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchReviewThread(string Id,
                                           int Number,
                                           string PageKey,
                                           string Query,
                                           WorkbenchReviewAnchor Anchor,
                                           WorkbenchReviewStatus Status,
                                           DateTimeOffset CreatedAt,
                                           long Sequence,
                                           bool HasState,
                                           WorkbenchReviewMessage[] Messages,
                                           WorkbenchReviewTask[] Tasks)
{
    public WorkbenchScenario Scenario => WorkbenchScenario.Parse(Query);
}
