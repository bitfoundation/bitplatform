namespace Bit.Butil;

/// <summary>
/// How <see cref="ElementReferenceExtensions.Focus(Microsoft.AspNetCore.Components.ElementReference, FocusOptions?)"/>
/// should behave beyond moving focus.
/// </summary>
public class FocusOptions
{
    /// <summary>
    /// True to leave the scroll position alone. By default the browser scrolls the newly focused
    /// element into view, which is wrong for focus moved programmatically during an animation or
    /// while restoring state.
    /// </summary>
    public bool? PreventScroll { get; set; }

    /// <summary>
    /// Whether the focus ring should be drawn, overriding the browser's own heuristic. Firefox only;
    /// ignored elsewhere.
    /// </summary>
    public bool? FocusVisible { get; set; }

    internal FocusJsOptions ToJsObject() => new()
    {
        PreventScroll = PreventScroll,
        FocusVisible = FocusVisible
    };
}
