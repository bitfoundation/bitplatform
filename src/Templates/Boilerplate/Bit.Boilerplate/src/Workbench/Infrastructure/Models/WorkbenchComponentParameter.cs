namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A parameter of a picked component, with its value in the view and how the tag sets it.</summary>
public sealed record WorkbenchComponentParameter
{
    public required string Name { get; init; }

    public required string TypeName { get; init; }

    public WorkbenchParameterEditor Editor { get; init; }

    public string? Value { get; init; }

    public string? DefaultValue { get; init; }

    /// <summary>The names an enum parameter can take.</summary>
    public string[] Choices { get; init; } = [];

    /// <summary>Whether null is a value of its own, shown as Not set.</summary>
    public bool IsNullable { get; init; }

    /// <summary>The base component that declares it, like BitComponentBase; null for the component's own parameters.</summary>
    public string? InheritedFrom { get; init; }

    /// <summary>Whether the tag sets it with a value this panel can change.</summary>
    public bool IsSet { get; init; }

    /// <summary>The attribute's text when code sets it, like @Localizer[...]; such a value is read-only here.</summary>
    public string? Code { get; init; }

    /// <summary>The parts of a Styles or Classes parameter, each with its style or class.</summary>
    public WorkbenchComponentPart[] Parts { get; init; } = [];
}
