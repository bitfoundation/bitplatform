namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A page's review status, with who set it and when.</summary>
public sealed record WorkbenchPageReview(string PageKey, WorkbenchPageStatus Status, string? By, DateTimeOffset? At);
