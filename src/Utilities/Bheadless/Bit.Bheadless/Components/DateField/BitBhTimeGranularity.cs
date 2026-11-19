namespace Bit.Bheadless;

/// <summary>The smallest unit a <see cref="BitBhTimeField"/> or <see cref="BitBhDateTimeField"/> edits.</summary>
public enum BitBhTimeGranularity
{
    /// <summary>Hours only (minutes and seconds are 0).</summary>
    Hour,

    /// <summary>Hours and minutes.</summary>
    Minute,

    /// <summary>Hours, minutes and seconds.</summary>
    Second,
}
