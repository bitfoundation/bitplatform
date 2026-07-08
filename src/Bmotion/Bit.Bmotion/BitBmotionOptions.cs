namespace Bit.Bmotion;

/// <summary>
/// Library-wide options for Bit.Bmotion, configured at service registration:
/// <code>
/// builder.Services.AddBitBmotionServices(o =&gt; o.ReducedMotion = BmReducedMotionMode.User);
/// </code>
/// </summary>
public sealed class BitBmotionOptions
{
    /// <summary>
    /// How the library honors the OS <c>prefers-reduced-motion</c> accessibility preference.
    /// Defaults to <see cref="BmReducedMotionMode.IgnoreUnlessConfigured"/> for back-compat;
    /// <see cref="BmReducedMotionMode.User"/> is recommended for new apps (respects the OS setting
    /// everywhere without requiring a <see cref="BmotionConfig"/>). A local
    /// <c>&lt;BmotionConfig ReduceMotion="…"&gt;</c> always overrides this.
    /// </summary>
    public BmReducedMotionMode ReducedMotion { get; set; } = BmReducedMotionMode.IgnoreUnlessConfigured;

    /// <summary>
    /// Validation policy for string-valued CSS props written verbatim into inline style. Defaults to
    /// <see cref="BmCssSafeMode.Off"/>; set to <see cref="BmCssSafeMode.Warn"/> or
    /// <see cref="BmCssSafeMode.Throw"/> when binding untrusted end-user input to those props.
    /// </summary>
    public BmCssSafeMode CssSafeMode { get; set; } = BmCssSafeMode.Off;
}
