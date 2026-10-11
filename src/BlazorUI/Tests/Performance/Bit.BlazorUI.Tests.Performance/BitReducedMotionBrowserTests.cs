using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the one way a duration follows the reduced motion preference (#13552): a duration the
/// PAGE sets on a component - a public <c>--bit-&lt;Component&gt;-*-duration</c> variable, a duration parameter the
/// component writes inline - is scaled by <c>--bit-mot-duration-factor</c>, so it collapses with the theme's own
/// durations, and comes back with them inside a ForceAnimation subtree, on a component that sets ForceAnimation, and
/// wherever an app restores the tokens at <c>:root</c>.
///
/// What is asserted is what each engine computed: the longest transition or animation duration under every component,
/// read off the computed styles, which also proves every engine resolves the scaled <c>calc()</c> to a time (the end
/// of a Collapse or an Accordion is timed off it). Run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitReducedMotionBrowserTests
public class BitReducedMotionBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/reduced-motion";

    // The pace every component on the page is given, which no theme token has.
    private const double Pace = 1700;

    // A collapsed duration lands on 0.01ms; anything under a millisecond is one.
    private const double Collapsed = 1;

    private const string RestoreTokensAtRoot = @":root {
        --bit-mot-duration: var(--bit-mot-duration-full, 200ms);
        --bit-mot-duration-short: var(--bit-mot-duration-short-full, 100ms);
        --bit-mot-duration-long: var(--bit-mot-duration-long-full, 300ms);
        --bit-mot-duration-factor: var(--bit-mot-duration-factor-full, 1);
    }";

    private static readonly string[] Probes =
    [
        "toggle", "slider", "textfield", "progress", "overlay", "image", "collapse-param", "collapse-var",
        "accordion-param", "accordion-theme", "carousel", "snackbar", "chart", "chart-progressive"
    ];

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    [TestMethod]
    public async Task FullMotion_KeepsThePaceThePageSet()
    {
        await Open(ReducedMotion.NoPreference);

        await AssertPace("#plain", Pace);
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesThePaceThePageSet()
    {
        await Open(ReducedMotion.Reduce);

        await AssertPace("#plain", null);
    }

    [TestMethod]
    public async Task ReducedMotion_ForceAnimationOnAnAncestor_KeepsThePace()
    {
        await Open(ReducedMotion.Reduce);

        await AssertPace("#fam", Pace);
    }

    [TestMethod]
    public async Task ReducedMotion_ForceAnimationOnTheComponent_KeepsThePace()
    {
        await Open(ReducedMotion.Reduce);

        await AssertPace("#self", Pace);
    }

    [TestMethod]
    public async Task ReducedMotion_AnAppRestoringTheTokensAtRoot_KeepsThePace()
    {
        await Open(ReducedMotion.Reduce);
        await Page.AddStyleTagAsync(new() { Content = RestoreTokensAtRoot });

        await AssertPace("#plain", Pace);
    }

    [TestMethod]
    public async Task ThePreferenceIsFollowedLive()
    {
        // The factor is a token like the durations, so a preference switched while the page is open is followed at once.
        await Open(ReducedMotion.NoPreference);
        await AssertPace("#plain", Pace);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await AssertPace("#plain", null);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await AssertPace("#plain", Pace);
    }

    [TestMethod]
    public async Task ThePageSetDelaysCollapseWithTheDurations()
    {
        // A Collapse Delay, the chart's stagger and the points a progressive line reaches: kept with motion, gone without
        // it, since a transition that no longer plays has nothing to postpone.
        await Open(ReducedMotion.NoPreference);

        Assert.AreEqual(400, (await Measure("#plain", "collapse-param")).Delay, 1);
        Assert.AreEqual(600, (await Measure("#plain", "chart")).Delay, 1);
        Assert.IsTrue((await Measure("#plain", "chart-progressive")).Delay > 1000);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        foreach (var probe in new[] { "collapse-param", "chart", "chart-progressive" })
        {
            Assert.IsTrue((await Measure("#plain", probe)).Delay < Collapsed, $"{probe} still waits under reduced motion.");
        }

        Assert.AreEqual(400, (await Measure("#fam", "collapse-param")).Delay, 1);
        Assert.AreEqual(600, (await Measure("#self", "chart")).Delay, 1);
    }

    [DataTestMethod]
    [DataRow("NoPreference", "#swiper-plain", true)]
    [DataRow("Reduce", "#swiper-plain", false)]
    [DataRow("Reduce", "#swiper-fam", true)]
    public async Task TheSwiperMovesAtThePaceThePreferenceLeavesIt(string preference, string swiper, bool animates)
    {
        await Open(Enum.Parse<ReducedMotion>(preference));

        // Where the container is a third of the pace into the move: on its way with motion, already there without.
        var positions = await Page.EvaluateAsync<double[]>(@"async (root) => {
            const container = document.querySelector(root + ' .bit-swp-cnt');
            const t0 = performance.now();
            document.querySelector(root + ' .bit-swp-rbt').click();
            await new Promise(resolve => {
                const tick = () => performance.now() - t0 < 560 ? requestAnimationFrame(tick) : resolve();
                requestAnimationFrame(tick);
            });
            const midway = Math.abs(container.scrollLeft);
            await new Promise(resolve => setTimeout(resolve, 2000));
            return [midway, Math.abs(container.scrollLeft)];
        }", swiper);

        var (midway, end) = (positions[0], positions[1]);

        Assert.IsTrue(end > 0, $"The swiper never moved ({end}px).");

        if (animates)
        {
            Assert.IsTrue(midway > 0 && midway < end - 1, $"The swiper is not on its way midway ({midway}px of {end}px).");
        }
        else
        {
            Assert.AreEqual(end, midway, 1, $"The swiper is still moving under reduced motion ({midway}px of {end}px).");
        }
    }

    [DataTestMethod]
    [DataRow("NoPreference", "#plain", "smooth")]
    [DataRow("Reduce", "#plain", "auto")]
    [DataRow("Reduce", "#fam", "smooth")]
    [DataRow("Reduce", "#self", "smooth")]
    public async Task ThePivotScrollsItsHeadersSmoothlyOnlyWithMotion(string preference, string set, string expected)
    {
        await Open(Enum.Parse<ReducedMotion>(preference));

        var behavior = await Page.EvaluateAsync<string>(@"(set) =>
            getComputedStyle(document.querySelector(set + ' [data-probe=pivot] .bit-pvt-hct')).scrollBehavior", set);

        Assert.AreEqual(expected, behavior);
    }

    [DataTestMethod]
    [DataRow("NoPreference")]
    [DataRow("Reduce")]
    public async Task TheStylesheetOnlyQueriesStepAsideForForceAnimation(string preference)
    {
        // A skeleton and a shimmer stop, a refused dismissal rings instead of moving - and all of them step aside for
        // ForceAnimation, on an ancestor or on the element itself.
        var reduced = preference == "Reduce";

        await Open(Enum.Parse<ReducedMotion>(preference));

        var json = await Page.EvaluateAsync<string>(@"() => {
            const parts = {
                card: ['bit-crd-skb', el => getComputedStyle(el, '::after').animationName],
                grid: ['bit-dtg-skeleton', el => getComputedStyle(el).animationName],
                modal: ['bit-mdl-ctn bit-mdl-bna', el => getComputedStyle(el).animationName],
                dialog: ['bit-dlg-prv', el => getComputedStyle(el).outlineStyle, 'bit-dlg']
            };
            const result = {};
            for (const [name, [classes, read, root]] of Object.entries(parts)) {
                for (const [where, host, self] of [['plain', 'probes-plain', false], ['fam', 'probes-fam', false], ['self', 'probes-plain', true]]) {
                    const el = document.createElement('div');
                    el.className = classes + (self ? ' bit-fam' : '');
                    el.style.width = '40px';
                    el.style.height = '10px';
                    // A part that reads a variable of its component's root is put inside one.
                    const parent = root ? document.createElement('div') : null;
                    if (parent) parent.className = root;
                    document.getElementById(host).appendChild(parent ?? el);
                    if (parent) parent.appendChild(el);
                    result[name + '-' + where] = read(el);
                }
            }
            return JSON.stringify(result);
        }");

        var results = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;

        foreach (var where in new[] { "plain", "fam", "self" })
        {
            var stops = reduced && where == "plain";

            Assert.AreEqual(stops, results[$"card-{where}"] == "none", $"card-{where}: {results[$"card-{where}"]}");
            Assert.AreEqual(stops, results[$"grid-{where}"] == "none", $"grid-{where}: {results[$"grid-{where}"]}");
            Assert.AreEqual(stops, results[$"modal-{where}"].StartsWith("bit-mdl-refuse-ring"), $"modal-{where}: {results[$"modal-{where}"]}");
            Assert.AreEqual(stops, results[$"dialog-{where}"] == "solid", $"dialog-{where}: {results[$"dialog-{where}"]}");
        }
    }

    private async Task Open(ReducedMotion preference)
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = preference });
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        // The snack bar's items and the image's fade arrive with a render of their own after the first one.
        await Page.WaitForFunctionAsync(@"() =>
            document.querySelectorAll('[data-probe=snackbar] .bit-snb-itm').length === 3 &&
            document.querySelectorAll('[data-probe=image] .bit-img-fde').length === 3", null, new() { Timeout = DefaultTimeout });

        // A carousel writes the transition of its slides only once it moves.
        await Page.EvaluateAsync(@"() => document.querySelectorAll('[data-probe=carousel] :is(.bit-csl-lbt, .bit-csl-rbt)')
            .forEach(b => getComputedStyle(b).display !== 'none' && b.click())");
        await Page.WaitForFunctionAsync(@"() =>
            document.querySelectorAll('[data-probe=carousel] .bit-crsi[style*=""transition""]').length >= 6", null, new() { Timeout = DefaultTimeout });
    }

    // Every probe of a set paced as expected: the page's own pace, or collapsed (null).
    private async Task AssertPace(string set, double? pace)
    {
        var failures = new List<string>();

        foreach (var probe in Probes)
        {
            var measured = await Measure(set, probe);

            if (measured.Unparsed.Length > 0)
            {
                failures.Add($"{probe}: unparsed computed times {string.Join(" | ", measured.Unparsed)}");
            }

            var ok = pace is { } p ? Math.Abs(measured.Duration - p) < 1 : measured.Duration < Collapsed;

            if (ok is false) failures.Add($"{probe}: {measured.Duration}ms");
        }

        Assert.AreEqual(0, failures.Count, $"Expected {(pace is null ? "collapsed" : $"{pace}ms")} in {set}:\n" + string.Join("\n", failures));
    }

    // The longest duration and delay of a transition or a running animation under a probe, as the engine computed them.
    private async Task<Measured> Measure(string set, string probe)
    {
        var json = await Page.EvaluateAsync<string>(@"(root) => {
            const host = document.querySelector(root);
            const unparsed = [];
            const ms = (list) => list.split(',').map(v => {
                v = v.trim();
                const n = parseFloat(v);
                if (isNaN(n) || /^-?[\d.e+-]+m?s$/.test(v) === false) { unparsed.push(v); return 0; }
                return v.endsWith('ms') ? n : n * 1000;
            });
            let duration = 0, delay = 0;
            for (const el of [host, ...host.querySelectorAll('*')]) {
                for (const pseudo of [null, '::before', '::after']) {
                    const s = getComputedStyle(el, pseudo);
                    if (s.transitionProperty !== 'none') {
                        duration = Math.max(duration, ...ms(s.transitionDuration));
                        delay = Math.max(delay, ...ms(s.transitionDelay));
                    }
                    if (s.animationName && s.animationName !== 'none') {
                        duration = Math.max(duration, ...ms(s.animationDuration));
                        delay = Math.max(delay, ...ms(s.animationDelay));
                    }
                }
            }
            return JSON.stringify({ duration, delay, unparsed });
        }", $"{set} [data-probe={probe}]");

        return JsonSerializer.Deserialize<Measured>(json, _json)!;
    }

    private sealed record Measured(double Duration, double Delay, string[] Unparsed);
}
