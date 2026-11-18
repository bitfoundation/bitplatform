using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow;

/// <summary>
/// Infrastructure: what <see cref="BlowCanvas"/> tells every <see cref="BlowNodeRenderer"/> alike. Shared by all of
/// them and replaced only when something in it changes, so handing it to a thousand nodes costs one reference each
/// rather than a dozen boxed values. Not meant to be used directly.
/// </summary>
public sealed class BlowNodeRenderSettings
{
    /// <summary>The canvas's <c>NodeTemplate</c>, or null for the built-in nodes.</summary>
    public RenderFragment<BlowNode>? Template { get; init; }

    /// <summary>The canvas's graph version; see <see cref="BlowCascadingNames.GraphVersion"/>.</summary>
    public int RenderEpoch { get; init; }

    /// <summary>False takes every node out of the tab order (the canvas's focus and keyboard flags).</summary>
    public bool Focusable { get; init; } = true;

    /// <summary>The id of the element holding the keyboard instructions for a node, or null for none.</summary>
    public string? DescribedBy { get; init; }

    /// <summary>Raises a node above its peers while it is selected.</summary>
    public bool Elevate { get; init; }

    /// <summary>Raises a node while it is being dragged. Off only in manual z-index mode.</summary>
    public bool ElevateWhileDragging { get; init; } = true;

    /// <summary>The canvas's accessible-name strings.</summary>
    public BlowAriaLabels AriaLabels { get; init; } = BlowAriaLabels.Default;

    /// <summary>The key that starts a connection from a focused node, or null when connecting from the keyboard is off.</summary>
    public string? ConnectionKey { get; init; }

    /// <summary>A node's state - connections, validation issues - as of this graph version.</summary>
    internal Func<BlowNode, BlowNodeState>? State { get; init; }

    internal bool SameAs(RenderFragment<BlowNode>? template, int epoch, bool focusable, string? describedBy, bool elevate,
        bool elevateWhileDragging, BlowAriaLabels ariaLabels, string? connectionKey)
        => ReferenceEquals(Template, template) && RenderEpoch == epoch && Focusable == focusable && DescribedBy == describedBy
           && Elevate == elevate && ElevateWhileDragging == elevateWhileDragging && ReferenceEquals(AriaLabels, ariaLabels)
           && ConnectionKey == connectionKey;
}
