namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A resource entry whose text matches a text picked in Edit text mode.</summary>
public sealed record WorkbenchTextMatch(string Resource, string Key, string Value);
