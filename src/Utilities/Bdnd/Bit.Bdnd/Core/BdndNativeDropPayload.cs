namespace Bit.Bdnd;

/// <summary>Raw payload shape received from JS for a native drop.</summary>
public sealed class BdndNativeDropPayload
{
    /// <summary>The DataTransfer types of the drop.</summary>
    public string[] Types { get; set; } = [];

    /// <summary>The string data of the drop, keyed by DataTransfer type.</summary>
    public Dictionary<string, string> Data { get; set; } = [];

    /// <summary>The metadata of the dropped files.</summary>
    public BdndNativeFile[] Files { get; set; } = [];
}
