namespace Bit.Butil;

/// <summary>
/// One pressure sample for a hardware source.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PressureRecord">PressureRecord</see>
/// </summary>
public class PressureRecord
{
    /// <summary>
    /// The source the sample describes. <c>"cpu"</c> everywhere today; <c>"thermals"</c> is
    /// specified but not yet shipping.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// <c>"nominal"</c>, <c>"fair"</c>, <c>"serious"</c> or <c>"critical"</c>. The deliberately
    /// coarse scale is what keeps the API from being a timing side channel - treat
    /// <c>"serious"</c> as "shed optional work" and <c>"critical"</c> as "shed everything you can".
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// When the sample was taken, on the same clock as <c>performance.now()</c>, in milliseconds.
    /// </summary>
    public double Time { get; set; }
}
