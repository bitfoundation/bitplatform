namespace Bit.Bink;

/// <summary>
/// Describes what the current host/device can deliver. Applications use this to adapt their UI
/// (for example hiding a pressure toggle when no pressure-capable device has been seen).
/// Static capabilities come from feature detection; the <c>Observed*</c> members are updated as input arrives.
/// </summary>
public sealed record BinkInputCapabilities
{
    /// <summary>Capabilities of a host without any input support (headless engine, tests, server-side rendering).</summary>
    public static readonly BinkInputCapabilities None = new();

    /// <summary>The host supports Pointer Events.</summary>
    public bool PointerEvents { get; init; }

    /// <summary>The browser can deliver coalesced (high-frequency) samples between frames.</summary>
    public bool CoalescedEvents { get; init; }

    /// <summary>The browser can deliver predicted samples.</summary>
    public bool PredictedEvents { get; init; }

    /// <summary>The browser exposes <c>tiltX</c>/<c>tiltY</c> on pointer events.</summary>
    public bool Tilt { get; init; }

    /// <summary>The browser exposes <c>twist</c> on pointer events.</summary>
    public bool Twist { get; init; }

    /// <summary>Maximum simultaneous touch contacts reported by the host.</summary>
    public int MaxTouchPoints { get; init; }

    /// <summary>The device pixel ratio of the display the canvas is on.</summary>
    public double DevicePixelRatio { get; init; } = 1;

    /// <summary>The host supports <c>OffscreenCanvas</c>.</summary>
    public bool OffscreenCanvas { get; init; }

    /// <summary>The canvas was created with the low-latency (<c>desynchronized</c>) hint honored.</summary>
    public bool LowLatencyCanvas { get; init; }

    /// <summary>The engine has seen a pen pointer.</summary>
    public bool ObservedPen { get; init; }

    /// <summary>The engine has seen a touch pointer.</summary>
    public bool ObservedTouch { get; init; }

    /// <summary>The engine has seen pressure values other than the constant 0/0.5 mouse defaults, i.e. real pressure hardware.</summary>
    public bool ObservedPressure { get; init; }

    /// <summary>The engine has seen non-zero tilt values.</summary>
    public bool ObservedTilt { get; init; }

    /// <summary>Returns an updated copy reflecting a new sample. Cheap: only allocates when something changed.</summary>
    public BinkInputCapabilities Observe(in BinkInputSample sample)
    {
        bool pen = ObservedPen || sample.Kind == BinkPointerKind.Pen;
        bool touch = ObservedTouch || sample.Kind == BinkPointerKind.Touch;
        bool pressure = ObservedPressure || (sample.Kind == BinkPointerKind.Pen && sample.Pressure > 0 && Math.Abs(sample.Pressure - 0.5f) > 0.001f);
        bool tilt = ObservedTilt || sample.TiltX != 0 || sample.TiltY != 0;
        if (pen == ObservedPen && touch == ObservedTouch && pressure == ObservedPressure && tilt == ObservedTilt) return this;
        return this with { ObservedPen = pen, ObservedTouch = touch, ObservedPressure = pressure, ObservedTilt = tilt };
    }
}
