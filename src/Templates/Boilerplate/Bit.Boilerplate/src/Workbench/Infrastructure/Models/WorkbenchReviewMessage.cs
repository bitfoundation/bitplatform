namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A message in a comment's thread, from the reviewer or from the agent.</summary>
public sealed record WorkbenchReviewMessage(bool FromAgent, string Author, string Text, DateTimeOffset At);
