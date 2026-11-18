namespace Bit.BevTools;

public sealed class BevToolsLargeMessage
{
    /// <summary>"→" sent by the browser, "←" received.</summary>
    public string Direction { get; set; } = "";

    public string Kind { get; set; } = "";

    public long Bytes { get; set; }

    /// <summary>Milliseconds since the Unix epoch.</summary>
    public double At { get; set; }
}
