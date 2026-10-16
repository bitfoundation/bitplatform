namespace Bit.Bink;

/// <summary>
/// A single raw pointer sample as delivered by the host (browser). Samples are value types so a batch of
/// coalesced pointer events never allocates.
/// </summary>
public readonly record struct BinkInputSample
{
    /// <summary>The host's pointer identifier (stable for the lifetime of one contact).</summary>
    public int PointerId { get; init; }

    /// <summary>Device kind.</summary>
    public BinkPointerKind Kind { get; init; }

    /// <summary>Lifecycle phase.</summary>
    public BinkPointerPhase Phase { get; init; }

    /// <summary>X in view coordinates (CSS pixels relative to the canvas element).</summary>
    public float X { get; init; }

    /// <summary>Y in view coordinates (CSS pixels relative to the canvas element).</summary>
    public float Y { get; init; }

    /// <summary>Normalized pressure 0..1. Mice report 0.5 while pressed per the Pointer Events specification.</summary>
    public float Pressure { get; init; }

    /// <summary>Pen tilt around the Y axis in degrees (-90..90). 0 when unsupported.</summary>
    public float TiltX { get; init; }

    /// <summary>Pen tilt around the X axis in degrees (-90..90). 0 when unsupported.</summary>
    public float TiltY { get; init; }

    /// <summary>Pen barrel rotation in degrees (0..359). 0 when unsupported.</summary>
    public float Twist { get; init; }

    /// <summary>Timestamp in milliseconds (a monotonic clock such as <c>performance.now()</c>).</summary>
    public double Timestamp { get; init; }

    /// <summary>Bit mask of pressed buttons (Pointer Events <c>buttons</c>): 1 = primary, 2 = secondary, 4 = middle, 32 = pen eraser.</summary>
    public int Buttons { get; init; }

    /// <summary>True when the sample came from <c>getCoalescedEvents()</c> rather than a dispatched event.</summary>
    public bool IsCoalesced { get; init; }

    /// <summary>True when the sample is a browser prediction rather than a measured position.</summary>
    public bool IsPredicted { get; init; }

    /// <summary>True when the sample is the primary pointer of its kind (first finger, the mouse).</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Shift was held. Tools use it to constrain a gesture to a square, a circle or a fixed angle.</summary>
    public bool Shift { get; init; }

    /// <summary>Control (or Command on macOS) was held.</summary>
    public bool Ctrl { get; init; }

    /// <summary>Alt (or Option) was held.</summary>
    public bool Alt { get; init; }

    /// <summary>Position as a vector.</summary>
    public System.Numerics.Vector2 Position => new(X, Y);

    /// <summary>True when the pen's eraser end is in use (pen button mask 32).</summary>
    public bool IsEraserButton => (Buttons & 32) != 0;

    /// <summary>True when the secondary (right) button is in use.</summary>
    public bool IsSecondaryButton => (Buttons & 2) != 0;

    /// <summary>Number of double values used by <see cref="ReadFrom"/> / <see cref="WriteTo"/>.</summary>
    public const int PackedSize = 12;

    /// <summary>
    /// Decodes a sample from the packed wire layout used by the JavaScript input layer:
    /// [pointerId, kind, phase, x, y, pressure, tiltX, tiltY, twist, timestamp, buttons, flags].
    /// The flags field packs coalesced (1), predicted (2), primary (4), shift (8), control (16) and alt (32).
    /// </summary>
    public static BinkInputSample ReadFrom(ReadOnlySpan<double> data)
    {
        int flags = (int)data[11];
        return new BinkInputSample
        {
            PointerId = (int)data[0],
            Kind = (BinkPointerKind)(byte)Math.Clamp(data[1], 0, 3),
            Phase = (BinkPointerPhase)(byte)Math.Clamp(data[2], 0, 4),
            X = (float)data[3],
            Y = (float)data[4],
            Pressure = (float)Math.Clamp(data[5], 0, 1),
            TiltX = (float)Math.Clamp(data[6], -90, 90),
            TiltY = (float)Math.Clamp(data[7], -90, 90),
            Twist = (float)data[8],
            Timestamp = data[9],
            Buttons = (int)data[10],
            IsCoalesced = (flags & 1) != 0,
            IsPredicted = (flags & 2) != 0,
            IsPrimary = (flags & 4) != 0,
            Shift = (flags & 8) != 0,
            Ctrl = (flags & 16) != 0,
            Alt = (flags & 32) != 0,
        };
    }

    /// <summary>Encodes the sample into the packed wire layout. See <see cref="ReadFrom"/>.</summary>
    public void WriteTo(Span<double> data)
    {
        data[0] = PointerId;
        data[1] = (byte)Kind;
        data[2] = (byte)Phase;
        data[3] = X;
        data[4] = Y;
        data[5] = Pressure;
        data[6] = TiltX;
        data[7] = TiltY;
        data[8] = Twist;
        data[9] = Timestamp;
        data[10] = Buttons;
        data[11] = (IsCoalesced ? 1 : 0) | (IsPredicted ? 2 : 0) | (IsPrimary ? 4 : 0) | (Shift ? 8 : 0) | (Ctrl ? 16 : 0) | (Alt ? 32 : 0);
    }
}
