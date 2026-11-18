using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

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
