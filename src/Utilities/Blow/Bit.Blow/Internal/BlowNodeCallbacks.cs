using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow;

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
