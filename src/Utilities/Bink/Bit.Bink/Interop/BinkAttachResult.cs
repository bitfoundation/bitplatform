namespace Bit.Bink;

/// <summary>Size and capability information reported by the browser.</summary>
internal sealed record BinkAttachResult
{
    public double Width { get; init; }
    public double Height { get; init; }
    public double DevicePixelRatio { get; init; } = 1;
    public int MaxTouchPoints { get; init; }
    public bool CoalescedEvents { get; init; }
    public bool PredictedEvents { get; init; }
    public bool PointerEvents { get; init; }
    public bool Tilt { get; init; }
    public bool Twist { get; init; }
    public bool OffscreenCanvas { get; init; }
    public bool LowLatency { get; init; }

    public BinkInputCapabilities ToCapabilities() => new()
    {
        PointerEvents = PointerEvents,
        CoalescedEvents = CoalescedEvents,
        PredictedEvents = PredictedEvents,
        Tilt = Tilt,
        Twist = Twist,
        MaxTouchPoints = MaxTouchPoints,
        DevicePixelRatio = DevicePixelRatio <= 0 ? 1 : DevicePixelRatio,
        OffscreenCanvas = OffscreenCanvas,
        LowLatencyCanvas = LowLatency,
    };
}
