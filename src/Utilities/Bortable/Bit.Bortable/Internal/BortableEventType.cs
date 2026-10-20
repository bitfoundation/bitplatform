namespace Bit.Bortable;

/// <summary>Identifies which lifecycle callback a zone should raise.</summary>
internal enum BortableEventType
{
    Start,
    End,
    Add,
    Remove,
    Update,
    Sort,
    Change
}
