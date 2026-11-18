namespace Bit.Blow;

/// <summary>The verdict on one prospective connection.</summary>
/// <param name="Rejection">Why it was refused, or <see cref="BlowConnectionRejection.None"/>.</param>
public readonly record struct BlowConnectionCheck(BlowConnectionRejection Rejection)
{
    /// <summary>The verdict for an allowed connection.</summary>
    public static readonly BlowConnectionCheck Valid = new(BlowConnectionRejection.None);

    /// <summary>True when the connection is allowed.</summary>
    public bool IsValid => Rejection == BlowConnectionRejection.None;
}
