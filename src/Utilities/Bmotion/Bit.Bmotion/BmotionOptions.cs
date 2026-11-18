namespace Bit.Bmotion;

/// <summary>
/// Library-wide options for Bit.Bmotion, configured at service registration:
/// <code>
/// builder.Services.AddBitBmotionServices(o =&gt; o.ReducedMotion = BmotionReducedMotionMode.User);
/// </code>
/// </summary>
public sealed class BmotionOptions
{
    /// <summary>
    /// How the library honors the OS <c>prefers-reduced-motion</c> accessibility preference.
    /// Defaults to <see cref="BmotionReducedMotionMode.IgnoreUnlessConfigured"/> for back-compat;
    /// <see cref="BmotionReducedMotionMode.User"/> is recommended for new apps (respects the OS setting
    /// everywhere without requiring a <see cref="BmotionConfig"/>). A local
    /// <c>&lt;BmotionConfig ReduceMotion="…"&gt;</c> always overrides this.
    /// </summary>
    public BmotionReducedMotionMode ReducedMotion { get; set; } = BmotionReducedMotionMode.IgnoreUnlessConfigured;

    /// <summary>
    /// Validation policy for string-valued CSS props written verbatim into inline style. Defaults to
    /// <see cref="BmotionCssSafeMode.Off"/>; set to <see cref="BmotionCssSafeMode.Warn"/> or
    /// <see cref="BmotionCssSafeMode.Throw"/> when binding untrusted end-user input to those props. It applies
    /// to every target a <see cref="Bmotion"/> element animates to, inherited variants included, and to
    /// the targets <see cref="BmotionAnimateService"/> animates.
    /// </summary>
    public BmotionCssSafeMode CssSafeMode { get; set; } = BmotionCssSafeMode.Off;

    /// <summary>
    /// What elements render while the page is prerendered or statically rendered on the server.
    /// Defaults to <see cref="BmotionPrerenderMode.Initial"/> (the <c>Initial</c> state, animating once
    /// interactive); <see cref="BmotionPrerenderMode.Final"/> renders visible content at rest and
    /// <see cref="BmotionPrerenderMode.Animate"/> plays the enter animation in CSS before the runtime loads.
    /// A <see cref="Bmotion.Prerender"/> set on an element overrides this.
    /// </summary>
    public BmotionPrerenderMode Prerender { get; set; } = BmotionPrerenderMode.Initial;

    /// <summary>
    /// Whether an element playing its <c>Exit</c> is made <c>inert</c> for the length of it: nothing in a closing
    /// dialog can be focused, clicked a second time or read out by a screen reader, and pointer events reach what is
    /// behind it. A cancelled exit (the element re-enters) makes it interactive again. Defaults to <c>true</c>; set
    /// <c>false</c> to keep exiting content interactive, as it was before.
    /// </summary>
    public bool InertWhileExiting { get; set; } = true;

    /// <summary>The names of the options <paramref name="other"/> sets differently.</summary>
    internal IEnumerable<string> DifferencesFrom(BmotionOptions other)
    {
        if (ReducedMotion != other.ReducedMotion)
        {
            yield return nameof(ReducedMotion);
        }

        if (CssSafeMode != other.CssSafeMode)
        {
            yield return nameof(CssSafeMode);
        }

        if (Prerender != other.Prerender)
        {
            yield return nameof(Prerender);
        }

        if (InertWhileExiting != other.InertWhileExiting)
        {
            yield return nameof(InertWhileExiting);
        }
    }
}
