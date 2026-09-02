namespace Bit.Butil;

/// <summary>
/// What the connection should be tuned for, as a hint to the browser's congestion controller.
/// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebTransport/WebTransport">the WebTransport constructor</see>.
/// </summary>
public enum WebTransportCongestionControl
{
    /// <summary>Leave the choice to the browser.</summary>
    Default,

    /// <summary>Optimize for bandwidth - a file transfer, where finishing sooner beats arriving sooner.</summary>
    Throughput,

    /// <summary>Optimize for delay - input, telemetry or media, where a late packet is a useless one.</summary>
    LowLatency
}
