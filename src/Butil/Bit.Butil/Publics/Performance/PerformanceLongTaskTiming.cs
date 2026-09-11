namespace Bit.Butil;

/// <summary>
/// A task that occupied the main thread for more than 50 ms - long enough that anything the user
/// did during it went unanswered.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceLongTaskTiming">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceLongTaskTiming</see>
/// </summary>
/// <remarks>
/// Long tasks are reported to a <c>PerformanceObserver</c> as they happen and are not kept in the
/// timeline buffer, so <see cref="Performance.GetLongTasks"/> reads what an observer was there to
/// catch: its own first call starts one, and only tasks after that are counted. Call it - or
/// subscribe - early if you want them all.
/// <br/>
/// The name is always <c>"self"</c> or the frame the task is attributed to; the interesting field is
/// <see cref="PerformanceEntry.Duration"/>. Use <see cref="PerformanceLongAnimationFrameTiming"/>
/// where it is supported - it names the script that did the blocking.
/// </remarks>
public class PerformanceLongTaskTiming : PerformanceEntry
{
    /// <summary>The browsing context the task is blamed on. Usually a single entry, and never more precise than a frame.</summary>
    public PerformanceTaskAttributionTiming[] Attribution { get; set; } = [];
}
