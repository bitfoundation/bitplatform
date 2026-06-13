namespace Bit.Bmotion.Models;

/// <summary>
/// Options for the <c>Layout</c> animation feature on a Motion component.
/// When <see cref="Enabled"/> is true a FLIP animation plays whenever the element
/// changes its position or size in the document layout.
/// </summary>
public class LayoutOptions
{
    /// <summary>Enable automatic layout animations. Default: false.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Unique identifier used for shared-element (cross-component) layout transitions.
    /// Two Motion components with the same LayoutId will animate between each other
    /// when one mounts and the other unmounts.
    /// </summary>
    public string? LayoutId { get; set; }

    /// <summary>
    /// Transition to use for the layout animation.
    /// Defaults to a snappy spring.
    /// </summary>
    public TransitionConfig? Transition { get; set; }
}
