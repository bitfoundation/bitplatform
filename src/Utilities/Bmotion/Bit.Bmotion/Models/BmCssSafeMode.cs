namespace Bit.Bmotion;

/// <summary>
/// How Bit.Bmotion treats string-valued CSS props (colors, dimensions, shadows, filters, CSS vars)
/// that fail the conservative injection check. Set via <see cref="BitBmotionOptions.CssSafeMode"/>.
/// Off by default for performance/parity; enable it when binding untrusted end-user input.
/// </summary>
public enum BmCssSafeMode
{
    /// <summary>No validation. String values are written verbatim (the default and historical behavior).</summary>
    Off = 0,

    /// <summary>Validate and log a warning for a rejected value, but still apply it.</summary>
    Warn = 1,

    /// <summary>Validate and throw for a rejected value.</summary>
    Throw = 2,
}
