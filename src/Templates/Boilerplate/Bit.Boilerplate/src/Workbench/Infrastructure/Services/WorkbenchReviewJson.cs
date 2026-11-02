namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Shapes the review into the JSON the agent reads, over MCP and HTTP alike.</summary>
public static class WorkbenchReviewJson
{
    public static object Describe(IEnumerable<WorkbenchReviewThread> threads, Dictionary<string, WorkbenchPageReview>? pages, long cursor, IEnumerable<WorkbenchFlow>? flows = null)
    {
        return new
        {
            Cursor = cursor,
            Comments = threads.Select(Describe).ToArray(),
            Pages = pages?.Values.Select(page => new { Page = page.PageKey, Status = page.Status.ToString().ToLowerInvariant(), page.By, page.At }).ToArray(),
            Flows = flows?.Select(flow => new
            {
                flow.Id,
                flow.Name,
                Board = WorkbenchUrls.Flow(flow.Id),
                Screens = flow.Screens.Select(screen => new { screen.Id, Page = screen.PageKey, screen.Query, screen.Note, State = screen.HasState }).ToArray(),
                Links = flow.Links.Select(link => new { link.From, link.To, link.Label }).ToArray()
            }).ToArray()
        };
    }

    /// <summary>A comment with its element, its view, links to it, its messages and its tasks.</summary>
    private static object Describe(WorkbenchReviewThread thread)
    {
        var scenario = thread.Scenario;
        var device = scenario.DevicePreset;
        var (width, height) = device.GetViewport(scenario.Rotated);

        return new
        {
            thread.Id,
            Page = thread.PageKey,
            thread.Number,
            Status = thread.Status.ToString().ToLowerInvariant(),
            Element = new { thread.Anchor.Component, thread.Anchor.Text, thread.Anchor.Selector, thread.Anchor.X, thread.Anchor.Y },
            View = new
            {
                Device = device.Key == DevicePresets.CustomKey ? "custom" : device.Name,
                Width = width,
                Height = height,
                Theme = scenario.Theme.ToString().ToLowerInvariant(),
                Culture = scenario.Culture ?? CultureInfoManager.DefaultCulture.Name,
                Data = scenario.Data.ToString().ToLowerInvariant(),
                SignIn = scenario.Auth.ToString().ToLowerInvariant(),
                User = scenario.Auth is WorkbenchAuthState.SignedOut ? null : WorkbenchDemoUsers.Find(scenario.User).Email,
                scenario.Variant,
                Query = thread.Query
            },
            Links = new
            {
                Workbench = $"{WorkbenchUrls.Workbench(thread.PageKey)}{thread.Query}",
                Screenshot = $"/api/review/{thread.Id}/screenshot"
            },
            Messages = thread.Messages.Select(message => new { From = message.FromAgent ? "agent" : "reviewer", message.Author, message.Text, message.At }).ToArray(),
            Tasks = thread.Tasks.Select(task => new { task.Text, task.Done }).ToArray()
        };
    }
}
