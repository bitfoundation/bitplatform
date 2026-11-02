using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.Bmotion.Tests.E2E;

/// <summary>
/// How faithfully an animation follows what is really on screen: starting values the engine never set (stylesheet
/// colours and widths), colour syntax it has to parse, velocity carried into a retargeted spring, and overlays that
/// must animate on every runtime rather than only on the one with a frame loop.
/// </summary>
[TestClass]
public sealed partial class FidelityTests : BmotionPageTest
{
    protected override string Route => "fidelity";

    [TestMethod]
    public async Task HoverColour_StartsFromTheStylesheetColour_AndGivesItBack()
    {
        await ExpectEventAsync("armed:css-hover");

        await HoverAsync("#css-hover");
        var samples = await SampleAsync("#css-hover", 900);
        var colours = samples.Select(s => Rgba(s.BackgroundColor)).ToArray();

        // Never through transparent black (the engine's old guess), always somewhere between blue and red.
        Assert.IsTrue(colours.All(c => c.A > 0.99 && c.G < 2), $"Left the blue-red line: {Describe(samples)}");
        Assert.IsTrue(colours.Any(c => c.R is > 40 and < 215 && c.B is > 40 and < 215), $"No intermediate frame: {Describe(samples)}");
        await WaitForStateAsync("#css-hover", "s => s.backgroundColor === 'rgb(255, 0, 0)'");

        await Page.Mouse.MoveAsync(1, 1);
        await ExpectEventAsync("css-hover:end");
        await WaitForStateAsync("#css-hover", "s => s.backgroundColor === 'rgb(0, 0, 255)'");
        // Handed back to the stylesheet rather than pinned inline, so a later stylesheet change still applies.
        await WaitForStateAsync("#css-hover", "s => !/background/.test(s.style)");
    }

    [TestMethod]
    public async Task HoverShadow_GrowsFromTheStylesheetShadow_AndShrinksBack()
    {
        await ExpectEventAsync("armed:css-shadow");

        await HoverAsync("#css-shadow");
        var growing = await SampleStyleAsync("#css-shadow", "box-shadow", 900);
        var blurs = growing.Select(s => ShadowBlur(s.Value)).ToArray();
        // The page reports the stylesheet's shadow colour first and with its spread; the hover's is authored the
        // other way round. Unaligned, the two don't mix and the shadow jumps.
        Assert.IsTrue(blurs.All(b => b >= 3.9), $"Dropped below the stylesheet's 4px blur: {Describe(growing)}");
        Assert.IsTrue(blurs.Any(b => b is > 8 and < 28), $"4px → 32px jumped: {Describe(growing)}");
        await WaitForStyleAsync("#css-shadow", "box-shadow", "v => / 32px /.test(v)");

        await Page.Mouse.MoveAsync(1, 1);
        await ExpectEventAsync("css-shadow:end");
        await WaitForStyleAsync("#css-shadow", "box-shadow", "v => / 4px /.test(v)");
        await WaitForStateAsync("#css-shadow", "s => !/box-shadow/.test(s.style)");
    }

    [TestMethod]
    public async Task HoverFilter_GrowsOutOfNone()
    {
        await ExpectEventAsync("armed:css-filter");

        await HoverAsync("#css-filter");
        var samples = await SampleStyleAsync("#css-filter", "filter", 900);

        Assert.IsTrue(samples.Any(s => BlurOf(s.Value) is > 1 and < 5), $"none → blur(6px) jumped: {Describe(samples)}");
        await WaitForStyleAsync("#css-filter", "filter", "v => v === 'blur(6px)'");
    }

    [TestMethod]
    public async Task Width_AnimatesFromTheStylesheetWidth()
    {
        await ExpectEventAsync("armed:css-width");
        Assert.AreEqual(100, (await ProbeAsync("#css-width")).Width, 0.5);

        await ClickAsync("widen");
        var samples = await SampleAsync("#css-width", 900);

        Assert.IsTrue(samples.Any(s => s.Width is > 120 and < 280), $"No intermediate width: {Describe(samples)}");
        Assert.IsTrue(samples.All(s => s.Width >= 99.5), $"Started below the stylesheet width: {Describe(samples)}");
        await WaitForStateAsync("#css-width", "s => Math.abs(s.width - 300) < 0.5");
    }

    [TestMethod]
    public async Task Height_OpensToItsContent_FollowsIt_AndClosesFromWhereItIs()
    {
        await ExpectEventAsync("armed:accordion");
        Assert.AreEqual(0, (await ProbeAsync("#accordion")).Height, 0.5);

        await Page.EvaluateAsync("() => setTimeout(() => document.getElementById('accordion-toggle').click(), 50)");
        var opening = await SampleOnTheTimelineAsync("#accordion", 900);
        Assert.IsTrue(opening.Any(s => s.Height is > 15 and < 65), $"0 → auto jumped: {Describe(opening)}");
        Assert.IsTrue(opening.All(s => s.Height <= 80.5), $"Overshot its content: {Describe(opening)}");
        await ExpectEventAsync("accordion:opened");
        await WaitForStateAsync("#accordion", "s => Math.abs(s.height - 80) < 0.5 && /height: auto/.test(s.style)");

        // auto, not 80px: it follows what it holds.
        await ClickAsync("accordion-grow");
        await WaitForStateAsync("#accordion", "s => Math.abs(s.height - 100) < 0.5");

        // Every frame, in the page: closing from 80 (the height it opened to) would jump past 82-99.5 in one go.
        await Page.EvaluateAsync("() => setTimeout(() => document.getElementById('accordion-toggle').click(), 50)");
        var closing = await SampleOnTheTimelineAsync("#accordion", 1200);
        Assert.IsTrue(closing.Any(s => s.Height is > 82 and < 99.5), $"Didn't close from its grown height: {Describe(closing)}");
        await WaitForStateAsync("#accordion", "s => s.height < 0.5");
    }

    [TestMethod]
    public async Task NamedColours_Interpolate()
    {
        await ExpectEventAsync("armed:named");
        await WaitForStateAsync("#named", "s => s.backgroundColor === 'rgb(255, 0, 0)'");

        await ClickAsync("recolor");
        var samples = await SampleAsync("#named", 900);

        Assert.IsTrue(samples.Select(s => Rgba(s.BackgroundColor)).Any(c => c.R is > 40 and < 215 && c.B is > 40 and < 215),
            $"red → blue jumped: {Describe(samples)}");
        await WaitForStateAsync("#named", "s => s.backgroundColor === 'rgb(0, 0, 255)'");
    }

    [TestMethod]
    public async Task ModernColourSyntax_Interpolates()
    {
        await ExpectEventAsync("armed:modern");

        await ClickAsync("recolor-modern");
        var samples = await SampleAsync("#modern", 900);

        // An animated colour passes through many values; a snap shows at most the two ends.
        var distinct = samples.Select(s => s.BackgroundColor).Distinct().Count();
        Assert.IsTrue(distinct > 4, $"black → oklch(...) jumped ({distinct} distinct values): {Describe(samples)}");
    }

    [TestMethod]
    public async Task HoverOverlay_AnimatesThroughIntermediateFrames_OnEveryRuntime()
    {
        await ExpectEventAsync("armed:slow-hover");

        await HoverAsync("#slow-hover");
        var samples = await SampleAsync("#slow-hover", 700);

        Assert.IsTrue(samples.Any(s => s.ScaleX is > 1.08 and < 1.42), $"The hover overlay snapped: {Describe(samples)}");
        await WaitForStateAsync("#slow-hover", "s => Math.abs(s.scaleX - 1.5) < 0.01");
    }

    [TestMethod]
    public async Task ReducedMotion_KeepsTheFade_AndSnapsTheMove()
    {
        await ExpectEventAsync("armed:reduced");

        await ClickAsync("reduced-toggle");
        var samples = await SampleAsync("#reduced", 700);

        Assert.IsTrue(samples.Any(s => s.Opacity is > 0.3 and < 0.9), $"The fade snapped: {Describe(samples)}");
        Assert.IsTrue(samples.All(s => Math.Abs(s.X) < 0.5 || Math.Abs(s.X - 200) < 0.5), $"The move animated: {Describe(samples)}");
        await WaitForStateAsync("#reduced", "s => Math.abs(s.opacity - 0.2) < 0.01 && Math.abs(s.x - 200) < 0.5");
    }

    [TestMethod]
    public async Task RetargetedSpring_CarriesItsVelocity()
    {
        await ExpectEventAsync("armed:handoff");

        // Send it right, and while it is still accelerating send it back - sampled across the change, in one go, so
        // the moment the new animation takes over is among the frames.
        await ClickAsync("handoff-right");
        await WaitForStateAsync("#handoff", "s => s.x > 15");
        await Page.EvaluateAsync("() => setTimeout(() => document.getElementById('handoff-left').click(), 120)");
        var samples = await SampleOnTheTimelineAsync("#handoff", 900);

        // Find where the rightward speed first drops below 80% of its peak: the retarget taking hold (the spring is
        // still accelerating towards 300 when it is sent back, so nothing else slows it). A spring restarted from rest
        // turns round right there - it never gets further right. One that inherits its velocity has only begun to slow
        // down there: the soft spring (k 20, c 4) needs most of another 100 ms to stop it, 10-20 px further on.
        // Distances, not per-frame rates, and caught early in the slowdown, so a suite running at 30 fps under load
        // decides it the same way as one at 60.
        var peakSpeed = 0.0;
        var slowed = -1;
        for (var i = 1; i < samples.Count && slowed < 0; i++)
        {
            var v = (samples[i].X - samples[i - 1].X) / Math.Max(samples[i].Time - samples[i - 1].Time, 1);
            if (peakSpeed > 0.2 && v < peakSpeed * 0.8) slowed = i;
            peakSpeed = Math.Max(peakSpeed, v);
        }
        Assert.IsTrue(slowed > 0, $"The retarget never slowed it down: {Describe(samples)}");
        var further = samples.Skip(slowed).Max(s => s.X) - samples[slowed].X;
        var around = string.Join(" ", samples.Skip(Math.Max(0, slowed - 3)).Take(8).Select(s => $"{s.Time:0.0}:{s.X:0.##}"));
        Assert.IsTrue(further > 4, $"It travelled only {further:0.##} px further after slowing [{around}]: {Describe(samples)}");
        await WaitForStateAsync("#handoff", "s => Math.abs(s.x) < 1");
    }

    [TestMethod]
    public async Task NativeButton_KeepsItsKeyboardActivation()
    {
        await ExpectEventAsync("armed:native");
        await Page.Locator("#native").FocusAsync();

        await Page.Keyboard.PressAsync(" ");
        await ExpectEventAsync("native:click");
        await ExpectEventAsync("native:tap");

        await Page.Keyboard.PressAsync("Enter");
        await ExpectEventAsync("native:click", 2);
        await ExpectEventAsync("native:tap", 2);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    // One computed property, every frame, in the page (a Blazor Server round trip can't starve it).
    private async Task<IReadOnlyList<(double Time, string Value)>> SampleStyleAsync(string selector, string property, int milliseconds)
    {
        var json = await Page.EvaluateAsync<System.Text.Json.JsonElement>("""
            async ([sel, prop, ms]) => {
                const el = document.querySelector(sel);
                const out = [];
                const start = document.timeline.currentTime;
                while (document.timeline.currentTime - start < ms) {
                    out.push({ t: document.timeline.currentTime - start, v: getComputedStyle(el).getPropertyValue(prop) });
                    await new Promise(r => requestAnimationFrame(r));
                }
                return out;
            }
            """, new object[] { selector, property, milliseconds });
        return [.. json.EnumerateArray().Select(s => (s.GetProperty("t").GetDouble(), s.GetProperty("v").GetString() ?? ""))];
    }

    private Task WaitForStyleAsync(string selector, string property, string predicate)
        => Page.WaitForFunctionAsync(
            $"([sel, prop]) => ({predicate})(getComputedStyle(document.querySelector(sel)).getPropertyValue(prop))",
            new object[] { selector, property }, new() { Timeout = DefaultTimeout });

    private static string Describe(IReadOnlyList<(double Time, string Value)> samples)
        => string.Join(" | ", samples.Where((_, i) => i % 4 == 0).Select(s => $"{s.Time:0}ms {s.Value}"));

    // The blur radius of a computed box-shadow ("rgba(0, 0, 0, 0.3) 0px 2px 4px 0px").
    private static double ShadowBlur(string css)
    {
        var m = Regex.Match(css, @"\)\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+(-?[\d.]+)px");
        return m.Success ? double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : double.NaN;
    }

    private static double BlurOf(string css)
    {
        var m = Regex.Match(css, @"blur\((-?[\d.]+)px\)");
        return m.Success ? double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : double.NaN;
    }

    internal static (double R, double G, double B, double A) Rgba(string css)
    {
        var m = RgbRegex().Match(css);
        if (!m.Success) return (double.NaN, double.NaN, double.NaN, double.NaN);
        double At(int i) => double.Parse(m.Groups[i].Value, CultureInfo.InvariantCulture);
        return (At(1), At(2), At(3), m.Groups[4].Success ? At(4) : 1);
    }

    internal static string Describe(IReadOnlyList<MotionState> samples)
        => string.Join(" | ", samples.Where((_, i) => i % 4 == 0).Select(s => $"{s.Time:0}ms x={s.X:0.#} s={s.ScaleX:0.##} o={s.Opacity:0.##} w={s.Width:0} h={s.Height:0} bg={s.BackgroundColor}"));

    [GeneratedRegex(@"rgba?\(\s*([\d.]+),\s*([\d.]+),\s*([\d.]+)(?:,\s*([\d.]+))?\s*\)")]
    private static partial Regex RgbRegex();
}

/// <summary>Touch input on the fidelity page: a finger has no hover, so a tap must not play the hover overlay.</summary>
[TestClass]
public sealed class FidelityTouchTests : BmotionPageTest
{
    protected override string Route => "fidelity";

    protected override void ConfigureContext(Microsoft.Playwright.BrowserNewContextOptions options) => options.HasTouch = true;

    [TestMethod]
    public async Task TouchTap_IsATap_NotAHover()
    {
        await ExpectEventAsync("armed:touch");
        var (x, y) = await CentreAsync("#touch");

        await Page.Touchscreen.TapAsync((float)x, (float)y);

        await ExpectEventAsync("touch:tap");
        await Page.WaitForTimeoutAsync(300);
        await ExpectEventAsync("touch:hover-start", 0);
    }
}
