namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// Where a comment is pinned: the element's CSS selector, component name and text, and the spot on it as fractions of its box.
/// </summary>
public sealed record WorkbenchReviewAnchor(string Selector, string? Component, string? Text, double X, double Y);
