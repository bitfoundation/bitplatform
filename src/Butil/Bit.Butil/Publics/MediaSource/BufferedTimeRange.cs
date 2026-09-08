namespace Bit.Butil;

/// <summary>
/// One contiguous stretch of buffered media, an entry of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/TimeRanges">TimeRanges</see> a
/// <see cref="SourceBufferHandle.GetBuffered"/> call reports.
/// </summary>
/// <remarks>
/// More than one range means there are gaps: a seek that landed outside what was appended, or a
/// segment that never arrived. Playback stops at the end of the range the current time sits in, so
/// the ranges are what a player watches to decide what to fetch next.
/// </remarks>
public class BufferedTimeRange
{
    /// <summary>Where the range starts, in seconds on the element's timeline.</summary>
    public double Start { get; set; }

    /// <summary>Where the range ends, in seconds on the element's timeline.</summary>
    public double End { get; set; }

    /// <summary>How much media the range holds, in seconds.</summary>
    public double Duration => End - Start;
}
