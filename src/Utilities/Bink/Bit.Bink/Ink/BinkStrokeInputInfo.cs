namespace Bit.Bink;

/// <summary>Metadata describing the device that produced a stroke.</summary>
public readonly record struct BinkStrokeInputInfo(BinkPointerKind Kind, bool HasRealPressure)
{
    /// <summary>Unknown device without pressure.</summary>
    public static readonly BinkStrokeInputInfo Unknown = new(BinkPointerKind.Unknown, false);
}
