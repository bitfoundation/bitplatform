namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>What's being typed in the review, for a new comment and for a reply.</summary>
public sealed class WorkbenchReviewDrafts
{
    public string? Comment { get; set; }

    public string? Reply { get; set; }
}
