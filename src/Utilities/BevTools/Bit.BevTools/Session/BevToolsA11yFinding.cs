namespace Bit.BevTools;

/// <summary>One finding of the page's accessibility check (page hub), with the component that rendered the element.</summary>
public sealed class BevToolsA11yFinding
{
    /// <summary>"control-name", "field-label", "image-alt", "hidden-focusable", "tabindex-positive", "duplicate-id", "heading-empty", "page-lang".</summary>
    public string Rule { get; set; } = "";

    /// <summary>The WCAG success criteria it is about ("4.1.2").</summary>
    public string Wcag { get; set; } = "";

    public string Message { get; set; } = "";

    /// <summary>The element, by tag, id or first class: never its content.</summary>
    public string Element { get; set; } = "";

    public string? TargetId { get; set; }

    public long? InstanceId { get; set; }

    public string? Component { get; set; }

    /// <summary>Why the component could not be found, when the element may be a component's (the lookup failed); null for static markup.</summary>
    public string? OwnerUnknown { get; set; }
}
