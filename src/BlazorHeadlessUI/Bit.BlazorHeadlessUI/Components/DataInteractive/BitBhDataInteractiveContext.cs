namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Exposes the current interactive state of a <see cref="BitBhDataInteractive"/>
/// wrapper to its render fragment, enabling consumers to conditionally render
/// content based on hover, focus, and active states.
///
/// Passed via <c>RenderFragment&lt;BitBhDataInteractiveContext&gt;</c> — consumers
/// can access it through the implicit <c>@context</c> variable or a named
/// <c>Context="ctx"</c> parameter.
/// </summary>
public sealed record BitBhDataInteractiveContext
{
    /// <summary>Whether the element is currently being hovered by the mouse (ignored on touch devices).</summary>
    public required bool Hover { get; init; }

    /// <summary>Whether the element has keyboard focus (focus-visible semantics).</summary>
    public required bool Focus { get; init; }

    /// <summary>Whether the element is currently being pressed/activated.</summary>
    public required bool Active { get; init; }

    /// <summary>Whether the element is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Shorthand: the element can receive interaction (not disabled).</summary>
    public bool Interactive => !Disabled;
}
