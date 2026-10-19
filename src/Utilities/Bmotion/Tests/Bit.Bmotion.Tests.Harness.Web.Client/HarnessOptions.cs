using Bit.Bmotion;

namespace BmotionTests.Harness.Web.Client;

/// <summary>
/// The library's global options (<see cref="BitBmotionOptions"/>) a harness host was started with. The server
/// reads them from configuration (<c>--BmotionHarness:Prerender=Animate</c>, ...) and writes them on
/// <c>&lt;body data-bm-*&gt;</c>, where the WebAssembly client reads them back: both containers must register the
/// library identically, or the prerendering pass and the runtime that takes over would disagree about what an
/// element looks like at rest.
/// </summary>
public sealed record HarnessOptions(BmPrerenderMode Prerender, BmReducedMotionMode ReducedMotion, BmCssSafeMode CssSafeMode)
{
    public const string PrerenderKey = "BmotionHarness:Prerender";
    public const string ReducedMotionKey = "BmotionHarness:ReducedMotion";
    public const string CssSafeModeKey = "BmotionHarness:CssSafeMode";

    public const string PrerenderAttribute = "data-bm-prerender";
    public const string ReducedMotionAttribute = "data-bm-reduced-motion";
    public const string CssSafeModeAttribute = "data-bm-css-safe-mode";

    public static HarnessOptions Default { get; } = new(BmPrerenderMode.Initial, BmReducedMotionMode.IgnoreUnlessConfigured, BmCssSafeMode.Off);

    /// <summary>Reads the options through <paramref name="read"/>; a missing value keeps the library's default.</summary>
    public static HarnessOptions Read(Func<string, string?> read) => new(
        Parse(read(PrerenderKey), Default.Prerender),
        Parse(read(ReducedMotionKey), Default.ReducedMotion),
        Parse(read(CssSafeModeKey), Default.CssSafeMode));

    public void Apply(BitBmotionOptions options)
    {
        options.Prerender = Prerender;
        options.ReducedMotion = ReducedMotion;
        options.CssSafeMode = CssSafeMode;
    }

    private static T Parse<T>(string? value, T fallback) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        // A typo must not quietly fall back to the default and let a whole suite pass against the wrong options.
        return Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Unknown {typeof(T).Name} '{value}'. Expected one of: {string.Join(", ", Enum.GetNames<T>())}.");
    }
}
