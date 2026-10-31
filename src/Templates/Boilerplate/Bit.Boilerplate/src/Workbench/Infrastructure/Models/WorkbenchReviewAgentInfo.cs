namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchReviewAgentInfo(string Name, string? ThreadId, DateTimeOffset At)
{
    public bool IsActive => DateTimeOffset.UtcNow - At < TimeSpan.FromMinutes(2);
}
