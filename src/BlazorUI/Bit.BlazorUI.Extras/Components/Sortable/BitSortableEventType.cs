namespace Bit.BlazorUI;

/// <summary>Identifies which lifecycle callback a zone should raise.</summary>
internal enum BitSortableEventType
{
    Start,
    End,
    Add,
    Remove,
    Update,
    Sort,
    Change
}
