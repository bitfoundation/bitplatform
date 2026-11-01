namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// A comment and its replies, pinned to an element of a view. Number counts the page's comments in the order they were made;
/// Sequence rises whenever the reviewer adds, answers or reopens it, and is what the agent's cursor follows.
/// </summary>
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
    /// <summary>The view the comment was left on.</summary>
    public WorkbenchScenario Scenario => WorkbenchScenario.Parse(Query);
}
