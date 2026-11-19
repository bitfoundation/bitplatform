using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

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
