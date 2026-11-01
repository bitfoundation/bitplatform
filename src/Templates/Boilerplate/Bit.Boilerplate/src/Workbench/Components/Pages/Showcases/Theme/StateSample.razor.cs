namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>
/// One state of a component on the Theme page. With <see cref="Force"/>, Scripts/states.ts draws the element inside
/// hovered, focused or pressed without touching it, and the sample is inert so it can't be used.
/// </summary>
public partial class StateSample
{
    /// <summary>The state's name as a resource key; <see cref="Name"/> is shown as written instead.</summary>
    [Parameter] public string? NameKey { get; set; }

    [Parameter] public string? Name { get; set; }

    /// <summary>The state to draw: hover, focus or active (pressed); null leaves the component as it is.</summary>
    [Parameter] public string? Force { get; set; }

    /// <summary>A CSS selector for the elements that can take the state; focusable elements when not set.</summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>Which of the matching elements takes the state, counting from 1; the first when not set.</summary>
    [Parameter] public int? Index { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
