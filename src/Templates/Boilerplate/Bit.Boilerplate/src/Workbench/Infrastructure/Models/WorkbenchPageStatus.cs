namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>Where a page's design stands: a draft, in review, approved, or built into the app.</summary>
public enum WorkbenchPageStatus
{
    Draft,
    InReview,
    Approved,
    Built
}
