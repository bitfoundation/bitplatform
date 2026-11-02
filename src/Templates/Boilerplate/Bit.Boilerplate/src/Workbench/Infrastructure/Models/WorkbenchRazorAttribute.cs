namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>An attribute of a tag in a .razor file: its value as written, without quotes, null when it has none, and where it sits.</summary>
public sealed record WorkbenchRazorAttribute(string Name, string? Value, int Start, int End);
