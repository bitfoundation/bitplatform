namespace Bit.Butil;

/// <summary>
/// What a capture track actually negotiated, read from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaStreamTrack/getSettings">MediaStreamTrack.getSettings()</see>.
/// </summary>
public class DisplayMediaSettings
{
    /// <summary>The track label - usually the name of the captured window, screen or tab.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>What the user picked: <c>"monitor"</c>, <c>"window"</c> or <c>"browser"</c>. Empty when the runtime doesn't report it.</summary>
    public string DisplaySurface { get; set; } = string.Empty;

    /// <summary>The negotiated frame width in pixels. 0 when unreported.</summary>
    public int Width { get; set; }

    /// <summary>The negotiated frame height in pixels. 0 when unreported.</summary>
    public int Height { get; set; }

    /// <summary>The negotiated frame rate. May differ from a requested one, and is 0 when unreported.</summary>
    public double FrameRate { get; set; }
}
