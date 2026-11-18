namespace Bit.Bheadless;

/// <summary>Whether a <see cref="BitBhTimeField"/> or <see cref="BitBhDateTimeField"/> shows a 12-hour clock (with AM / PM) or a 24-hour clock.</summary>
public enum BitBhHourCycle
{
    /// <summary>1–12 with an AM / PM segment.</summary>
    H12,

    /// <summary>0–23.</summary>
    H24,
}
