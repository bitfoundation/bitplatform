namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>One value of a page switch, with the resource key of its label.</summary>
public sealed record WorkbenchPageSwitchOption(string Value, string TextKey);
