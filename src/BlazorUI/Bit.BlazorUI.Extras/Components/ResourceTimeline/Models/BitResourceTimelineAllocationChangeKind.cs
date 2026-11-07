namespace Bit.BlazorUI;

/// <summary>
/// Why an allocation is being committed: a pointer/keyboard move or a resize.
/// </summary>
public enum BitResourceTimelineAllocationChangeKind
{
    /// <summary>The bar was moved in time and/or onto another resource.</summary>
    Move,

    /// <summary>The bar's start or end was resized.</summary>
    Resize,

    /// <summary>
    /// A new bar is being created. Not handed to the changing callbacks today: a bar drawn on empty content is
    /// created through <c>OnAllocationCreating</c> alone.
    /// </summary>
    Create
}
