namespace Bit.Bink;

/// <summary>Options passed to the browser layer when a canvas is attached.</summary>
internal sealed record BinkInteropOptions
{
    public bool LowLatency { get; init; }
    public bool RejectTouchWhilePen { get; init; } = true;
    public bool SuppressContextMenu { get; init; } = true;
    public bool TrackHover { get; init; }
    public bool PreventDefaultOnDown { get; init; } = true;
    public bool WetInk { get; init; }
    public bool HandleKeyboard { get; init; } = true;
    public int WheelMode { get; init; }
    public bool Clipboard { get; init; }
    public bool Predict { get; init; }
    public bool DropFiles { get; init; }
    public string[]? DisabledShortcuts { get; init; }
}
