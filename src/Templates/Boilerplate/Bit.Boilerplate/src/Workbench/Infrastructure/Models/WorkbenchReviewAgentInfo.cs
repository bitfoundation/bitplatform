namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>The agent last seen on the review, the comment it works on, and when; it counts as online for two minutes.</summary>
public sealed record WorkbenchReviewAgentInfo(string Name, string? ThreadId, DateTimeOffset At)
{
    public static readonly TimeSpan OnlineFor = TimeSpan.FromMinutes(2);

    public bool IsActive => DateTimeOffset.UtcNow - At < OnlineFor;
}
