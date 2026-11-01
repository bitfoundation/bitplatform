namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A resource entry's text in one culture, null when that culture has none.</summary>
public sealed record WorkbenchTextTranslation(string Culture, string DisplayName, string? Value);
