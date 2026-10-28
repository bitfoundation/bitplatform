using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A <c>keydown</c> seen by an element that hosts content of its own (a grid cell, a tree item), with the
/// element it belongs to. A key pressed in interactive content inside the host (an input, a button) bubbles
/// to the host too, but it is that content's key: <see cref="OwnerId"/> tells the two apart.
/// </summary>
public class BitBhKeyboardEventArgs : KeyboardEventArgs
{
    /// <summary>
    /// The id of the nearest host element (marked <c>data-bit-bh-event-owner</c>) the key was pressed on, or
    /// empty when it was pressed in interactive content nested inside one, or during an IME composition.
    /// </summary>
    public string? OwnerId { get; set; }
}

/// <summary>
/// A <c>click</c> seen by an element that hosts content of its own (a grid cell, a tree item), with the
/// element it belongs to: a click on a button inside a cell is the button's, not the cell's.
/// </summary>
public class BitBhMouseEventArgs : MouseEventArgs
{
    /// <summary>
    /// The id of the nearest host element (marked <c>data-bit-bh-event-owner</c>) the click landed in, or
    /// empty when it landed on interactive content nested inside one.
    /// </summary>
    public string? OwnerId { get; set; }
}

/// <summary>
/// The DOM events Bit.BlazorHeadlessUI registers with Blazor (from its JS initializer): <c>@onbitbhkeydown</c>
/// and <c>@onbitbhclick</c> fire with every <c>keydown</c> / <c>click</c> and say which host element owns it.
/// </summary>
[EventHandler("onbitbhkeydown", typeof(BitBhKeyboardEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onbitbhclick", typeof(BitBhMouseEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class BitBhEventHandlers
{
    /// <summary>The attribute that marks an element as the owner of the events inside it.</summary>
    public const string OwnerAttribute = "data-bit-bh-event-owner";

    /// <summary>The name of the owned <c>keydown</c> event in a render tree.</summary>
    public const string KeyDown = "onbitbhkeydown";

    /// <summary>The name of the owned <c>click</c> event in a render tree.</summary>
    public const string Click = "onbitbhclick";
}
