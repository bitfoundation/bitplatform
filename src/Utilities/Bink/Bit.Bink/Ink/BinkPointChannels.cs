namespace Bit.Bink;

/// <summary>Optional per-point channels a stroke may carry in addition to position, pressure and time.</summary>
[Flags]
public enum BinkPointChannels : byte
{
    /// <summary>Only X, Y, pressure and time.</summary>
    None = 0,
    /// <summary>Tilt X and tilt Y.</summary>
    Tilt = 1,
    /// <summary>Barrel rotation.</summary>
    Twist = 2,
}
