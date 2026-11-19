using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow;

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
