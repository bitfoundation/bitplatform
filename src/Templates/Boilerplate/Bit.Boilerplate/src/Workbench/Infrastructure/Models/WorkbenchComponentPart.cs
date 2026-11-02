namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>One part of a Styles or Classes parameter, like Root or Icon, with its style or class.</summary>
public sealed record WorkbenchComponentPart(string Name, string? Value);
