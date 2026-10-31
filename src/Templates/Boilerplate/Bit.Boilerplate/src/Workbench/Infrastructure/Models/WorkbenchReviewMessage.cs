namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchReviewMessage(bool FromAgent, string Author, string Text, DateTimeOffset At);
