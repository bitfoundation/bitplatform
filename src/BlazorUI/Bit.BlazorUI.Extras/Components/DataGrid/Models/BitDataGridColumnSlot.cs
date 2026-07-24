namespace Bit.BlazorUI;

/// <summary>
/// One horizontal render slot of a grid row while column virtualization is active: either a real
/// column (with its index among the visible columns) or a spacer standing in for a run of
/// scrolled-out columns, so the row keeps its full scroll width with far fewer DOM cells.
/// When column virtualization is off, the slots are simply all visible columns in order.
/// </summary>
public readonly record struct BitDataGridColumnSlot<TItem>(BitDataGridColumn<TItem>? Column, int ColIndex, double SpacerWidth);
