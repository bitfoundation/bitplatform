namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchPageReview(string PageKey, WorkbenchPageStatus Status, string? By, DateTimeOffset? At);
