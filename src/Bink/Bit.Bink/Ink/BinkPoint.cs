using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A single processed point of a stroke. This is a convenience view; strokes store points in
/// structure-of-arrays form (<see cref="BinkStrokePoints"/>) so reading points never allocates.
/// </summary>
public readonly record struct BinkPoint
{
    /// <summary>X in document units.</summary>
    public float X { get; init; }

    /// <summary>Y in document units.</summary>
    public float Y { get; init; }

    /// <summary>Normalized pressure 0..1 (0.5 when the device has no pressure).</summary>
    public float Pressure { get; init; }

    /// <summary>Milliseconds since the start of the stroke.</summary>
    public float Time { get; init; }

    /// <summary>Pen tilt around the Y axis in degrees, 0 when unavailable.</summary>
    public float TiltX { get; init; }

    /// <summary>Pen tilt around the X axis in degrees, 0 when unavailable.</summary>
    public float TiltY { get; init; }

    /// <summary>Pen barrel rotation in degrees, 0 when unavailable.</summary>
    public float Twist { get; init; }

    /// <summary>Position as a vector.</summary>
    public Vector2 Position => new(X, Y);

    /// <summary>Creates a point with position and pressure.</summary>
    public BinkPoint(float x, float y, float pressure = 0.5f, float time = 0f)
    {
        X = x;
        Y = y;
        Pressure = pressure;
        Time = time;
    }
}

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

/// <summary>Metadata describing the device that produced a stroke.</summary>
public readonly record struct BinkStrokeInputInfo(BinkPointerKind Kind, bool HasRealPressure)
{
    /// <summary>Unknown device without pressure.</summary>
    public static readonly BinkStrokeInputInfo Unknown = new(BinkPointerKind.Unknown, false);
}
