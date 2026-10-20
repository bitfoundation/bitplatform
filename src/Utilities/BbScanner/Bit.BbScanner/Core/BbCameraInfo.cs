using System.Text.Json.Serialization;

namespace Bit.BbScanner;

/// <summary>
/// What the browser actually negotiated, and what the running track can do.
/// </summary>
public sealed class BbCameraInfo
{
    /// <summary>Identifier of the open camera.</summary>
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Human readable name of the open camera.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Negotiated frame width in pixels.</summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>Negotiated frame height in pixels.</summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>Negotiated frame rate, when the browser reports one.</summary>
    [JsonPropertyName("frameRate")]
    public double FrameRate { get; set; }

    /// <summary>Reported facing mode, typically <c>environment</c> or <c>user</c>.</summary>
    [JsonPropertyName("facingMode")]
    public string FacingMode { get; set; } = string.Empty;

    /// <summary><see langword="true"/> when the track exposes a torch capability.</summary>
    [JsonPropertyName("supportsTorch")]
    public bool SupportsTorch { get; set; }

    /// <summary>Whether the torch is currently reported as on.</summary>
    [JsonPropertyName("torchOn")]
    public bool TorchOn { get; set; }

    /// <summary><see langword="true"/> when the track exposes a zoom capability.</summary>
    [JsonPropertyName("supportsZoom")]
    public bool SupportsZoom { get; set; }

    /// <summary>Smallest accepted zoom value.</summary>
    [JsonPropertyName("zoomMin")]
    public double ZoomMin { get; set; }

    /// <summary>Largest accepted zoom value.</summary>
    [JsonPropertyName("zoomMax")]
    public double ZoomMax { get; set; }

    /// <summary>Granularity of the zoom control, when reported.</summary>
    [JsonPropertyName("zoomStep")]
    public double ZoomStep { get; set; }

    /// <summary>Current zoom value.</summary>
    [JsonPropertyName("zoom")]
    public double Zoom { get; set; }
}
