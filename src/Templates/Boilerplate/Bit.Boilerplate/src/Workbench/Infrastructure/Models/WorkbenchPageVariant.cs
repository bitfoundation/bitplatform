namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A design option of a page that views can compare side by side, with the resource key of its name.</summary>
public sealed record WorkbenchPageVariant(string Key, string NameKey);
