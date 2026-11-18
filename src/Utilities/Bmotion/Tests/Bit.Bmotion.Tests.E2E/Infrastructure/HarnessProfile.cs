namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// The library's global options (<c>BmotionOptions</c>) a harness host process runs with. They are process-wide
/// - the library registers them as a singleton - so a fixture that needs options other than the defaults gets a
/// host process of its own (see <see cref="HarnessTarget"/>).
/// </summary>
/// <param name="Prerender">A <c>BmotionPrerenderMode</c> name.</param>
/// <param name="ReducedMotion">A <c>BmotionReducedMotionMode</c> name.</param>
/// <param name="CssSafeMode">A <c>BmotionCssSafeMode</c> name.</param>
/// <param name="JsCallTimeoutSeconds">The Server circuit's JS interop timeout in seconds; null keeps the framework's.</param>
public sealed record HarnessProfile(string Prerender, string ReducedMotion, string CssSafeMode, int? JsCallTimeoutSeconds = null)
{
    /// <summary>The library's defaults: what an app that calls <c>AddBitBmotionServices()</c> gets.</summary>
    public static HarnessProfile Default { get; } = new("Initial", "IgnoreUnlessConfigured", "Off");

    /// <summary>Every option moved off its default: prerendered CSS animations, the OS motion preference, CSS safe mode throwing.</summary>
    public static HarnessProfile Strict { get; } = new("Animate", "User", "Throw");

    /// <summary>Only the prerender mode moved: the takeover behaviour of <c>BmotionPrerenderMode.Final</c> globally.</summary>
    public static HarnessProfile PrerenderFinal { get; } = new("Final", "IgnoreUnlessConfigured", "Off");

    /// <summary>CSS safe mode warning instead of throwing: rejected values are logged and still applied.</summary>
    public static HarnessProfile CssWarn { get; } = new("Initial", "IgnoreUnlessConfigured", "Warn");

    /// <summary>The defaults, with the circuit giving up on a JS call after two seconds rather than a minute.</summary>
    public static HarnessProfile ShortJsCallTimeout { get; } = Default with { JsCallTimeoutSeconds = 2 };

    public IEnumerable<string> ToArguments() =>
    [
        $"--BmotionHarness:Prerender={Prerender}",
        $"--BmotionHarness:ReducedMotion={ReducedMotion}",
        $"--BmotionHarness:CssSafeMode={CssSafeMode}",
        .. JsCallTimeoutSeconds is { } seconds ? [$"--BmotionHarness:JsCallTimeoutSeconds={seconds}"] : Array.Empty<string>(),
    ];
}
