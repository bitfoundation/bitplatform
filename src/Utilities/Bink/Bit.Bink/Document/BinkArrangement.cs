namespace Bit.Bink;

/// <summary>Where <see cref="BinkDocument.Arrange"/> moves elements in their layer's stacking order.</summary>
public enum BinkArrangement : byte
{
    /// <summary>Above everything else in the layer.</summary>
    BringToFront = 0,
    /// <summary>One step up: above the next element that is not being moved.</summary>
    BringForward = 1,
    /// <summary>One step down: below the previous element that is not being moved.</summary>
    SendBackward = 2,
    /// <summary>Below everything else in the layer.</summary>
    SendToBack = 3,
}
