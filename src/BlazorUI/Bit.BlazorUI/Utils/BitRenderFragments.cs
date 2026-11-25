using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.BlazorUI;

/// <summary>
/// Tells whether a <see cref="RenderFragment"/> renders anything, for a decision a component has to make before it
/// renders one. A fragment that is set is not one that has content: a ChildContent written around an <c>@if</c> that is
/// false is set all the same, and renders nothing.
/// </summary>
internal static class BitRenderFragments
{
    /// <summary>
    /// Runs the fragment into a builder of its own and reports whether it wrote an element, a component, or text that is
    /// not whitespace.
    /// </summary>
    /// <remarks>
    /// Nothing the fragment writes there reaches a renderer - a component is not instantiated, an <c>@ref</c> is not
    /// captured - but the fragment's own code runs once more than it is rendered, so ask only when the answer is needed
    /// and nothing cheaper decides it. An element or a component counts as content whatever it holds, since what it
    /// renders is not known before it renders.
    /// </remarks>
    public static bool RendersContent(RenderFragment? fragment)
    {
        if (fragment is null) return false;

        using var builder = new RenderTreeBuilder();
        fragment(builder);

#pragma warning disable BL0006 // The frames are only read, to tell whether the fragment wrote anything.
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                case RenderTreeFrameType.Component:
                case RenderTreeFrameType.Text when string.IsNullOrWhiteSpace(frame.TextContent) is false:
                case RenderTreeFrameType.Markup when string.IsNullOrWhiteSpace(frame.MarkupContent) is false:
                    return true;
            }
        }
#pragma warning restore BL0006

        return false;
    }
}
