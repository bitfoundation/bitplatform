namespace Bit.Butil;

/// <summary>
/// How long one user interaction took to be answered - from the input arriving to the next frame
/// being painted. The raw material of Interaction to Next Paint.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceEventTiming">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceEventTiming</see>
/// </summary>
/// <remarks>
/// <see cref="PerformanceEntry.Duration"/> is the whole latency, rounded to 8 ms, and is the number
/// INP is computed from. The three parts are worth reading separately when it is bad:
/// <see cref="ProcessingStart"/> minus <see cref="PerformanceEntry.StartTime"/> is how long the
/// event waited for the main thread, <see cref="ProcessingEnd"/> minus
/// <see cref="ProcessingStart"/> is your handler, and what remains is the browser painting the
/// result.
/// <br/>
/// Only interactions slower than 104 ms are reported by default, so an empty list is the good case.
/// </remarks>
public class PerformanceEventTiming : PerformanceEntry
{
    /// <summary>When the event handlers started running.</summary>
    public double ProcessingStart { get; set; }

    /// <summary>When the event handlers finished.</summary>
    public double ProcessingEnd { get; set; }

    /// <summary>Whether the event was cancelable.</summary>
    public bool Cancelable { get; set; }

    /// <summary>
    /// An id shared by every event of one interaction - the <c>pointerdown</c>, <c>pointerup</c> and
    /// <c>click</c> of a single tap all carry the same one. <c>0</c> for events that are not part of
    /// an interaction.
    /// </summary>
    public long InteractionId { get; set; }
}
