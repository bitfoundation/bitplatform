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

/// <summary>
/// Infrastructure: the callbacks one <see cref="BlowNodeRenderer"/> raises into its canvas, made once per node.
/// Not meant to be used directly.
/// </summary>
public sealed class BlowNodeCallbacks
{
    /// <summary>A primary-button press on the node: selects it and may start a drag.</summary>
    public EventCallback<PointerEventArgs> DragStart { get; init; }

    /// <summary>Enter or Space on the focused node; the flag is "additive" (Shift held).</summary>
    public EventCallback<bool> Activate { get; init; }

    /// <summary>The connection key on the focused node. Empty while connecting from the keyboard is off.</summary>
    public EventCallback Connect { get; init; }

    /// <summary>The node took focus. Empty unless the canvas pans focused nodes into view or connects from the keyboard.</summary>
    public EventCallback<FocusEventArgs> Focus { get; init; }

    /// <summary>The DOM events of the node, each empty unless the app handles it.</summary>
    public EventCallback<MouseEventArgs> Click { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> DoubleClick { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> ContextMenu { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> MouseEnter { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> MouseMove { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> MouseLeave { get; init; }

    /// <summary>No callbacks at all, for a renderer used outside a canvas.</summary>
    public static readonly BlowNodeCallbacks None = new();
}

/// <summary>
/// Infrastructure: what <see cref="BlowCanvas"/> tells every <see cref="BlowEdgeRenderer"/> alike; see
/// <see cref="BlowNodeRenderSettings"/>. Not meant to be used directly.
/// </summary>
public sealed class BlowEdgeRenderSettings
{
    /// <summary>The canvas's <c>EdgeTemplate</c>, or null for the built-in edge.</summary>
    public RenderFragment<BlowEdgeContext>? Template { get; init; }

    /// <summary>True when the canvas draws labels in its HTML label layer instead.</summary>
    public bool SuppressLabel { get; init; }

    /// <summary>The canvas's graph version; see <see cref="BlowCascadingNames.GraphVersion"/>.</summary>
    public int RenderEpoch { get; init; }

    /// <summary>False takes every edge out of the tab order.</summary>
    public bool Focusable { get; init; } = true;

    /// <summary>The id of the element holding the keyboard instructions for an edge, or null for none.</summary>
    public string? DescribedBy { get; init; }

    /// <summary>The canvas's accessible-name strings.</summary>
    public BlowAriaLabels AriaLabels { get; init; } = BlowAriaLabels.Default;

    /// <summary>The key that picks up an end of a focused edge to move it, or null when that cannot be done from the keyboard.</summary>
    public string? ConnectionKey { get; init; }

    /// <summary>An edge's state - validation issues - as of this graph version.</summary>
    internal Func<BlowEdge, BlowEdgeState>? State { get; init; }

    /// <summary>The name a node is announced by, from its id: its aria label, label or id.</summary>
    internal Func<string, string>? NodeName { get; init; }

    internal bool SameAs(RenderFragment<BlowEdgeContext>? template, bool suppressLabel, int epoch, bool focusable,
        string? describedBy, BlowAriaLabels ariaLabels, string? connectionKey)
        => ReferenceEquals(Template, template) && SuppressLabel == suppressLabel && RenderEpoch == epoch
           && Focusable == focusable && DescribedBy == describedBy && ReferenceEquals(AriaLabels, ariaLabels)
           && ConnectionKey == connectionKey;
}

/// <summary>
/// Infrastructure: the callbacks one <see cref="BlowEdgeRenderer"/> raises into its canvas, made once per edge.
/// Not meant to be used directly.
/// </summary>
public sealed class BlowEdgeCallbacks
{
    /// <summary>Enter or Space on the focused edge; the flag is "additive" (Shift held).</summary>
    public EventCallback<bool> Activate { get; init; }

    /// <summary>A press on the edge: selects it.</summary>
    public EventCallback<PointerEventArgs> Select { get; init; }

    /// <summary>The connection key on the focused edge. Empty while reconnecting from the keyboard is off.</summary>
    public EventCallback Connect { get; init; }

    /// <summary>The DOM events of the edge, each empty unless the app handles it (a click always reports).</summary>
    public EventCallback<MouseEventArgs> Click { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> DoubleClick { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> ContextMenu { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> MouseEnter { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> MouseMove { get; init; }

    /// <inheritdoc cref="Click"/>
    public EventCallback<MouseEventArgs> MouseLeave { get; init; }

    /// <summary>No callbacks at all, for a renderer used outside a canvas.</summary>
    public static readonly BlowEdgeCallbacks None = new();
}
