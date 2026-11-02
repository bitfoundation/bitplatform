namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// A bit BlazorUI component picked in Inspect mode: its parameters as the view has them, and the tag in the code that sets
/// them, when it was found.
/// </summary>
public sealed record WorkbenchComponentInfo
{
    public required string Name { get; init; }

    /// <summary>The id of the component's root element in the view, to read the component again after a change.</summary>
    public required string ElementId { get; init; }

    public WorkbenchComponentTag? Tag { get; init; }

    /// <summary>The bit BlazorUI component that draws this one itself, so no tag in the app's code sets it.</summary>
    public string? DrawnBy { get; init; }

    public WorkbenchComponentParameter[] Parameters { get; init; } = [];
}
