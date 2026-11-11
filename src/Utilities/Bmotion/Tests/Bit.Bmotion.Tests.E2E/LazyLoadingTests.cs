using System.Text.Json;
using Bit.Utilities.Testing;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bmotion.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Bmotion on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Bmotion marked lazy and none of its
/// services registered in the browser - and driven in a browser. Its home page animates nothing and never downloads
/// <c>Bit.Bmotion.wasm</c>; its showcase has the router download it before the page renders, and the layout only that
/// page uses creates Bmotion's services with <c>ConfigureBmotionServices</c>: the page's card enters and grows under the
/// pointer, and its buttons animate from code through the service the page takes from
/// <c>GetBmotionService&lt;BmotionAnimateService&gt;()</c>. A deep link to the showcase is prerendered by the server, which
/// registers Bmotion's services with the same options and the <c>LazyAssemblyLoader</c> its router injects, and plays
/// the card's enter in CSS; the client's Program.cs downloads the assembly before the app starts, and the runtime takes
/// the card over without playing its enter again. Published with AOT (<c>BMOTION_E2E_LAZY_AOT=1</c>), the sample loads
/// Bit.Bmotion with the app, as the README says an AOT build must, and works just the same.
/// </summary>
/// <remarks>
/// The sample is an app of its own rather than a harness page, so this fixture does not derive from
/// <see cref="BmotionPageTest"/>: it opens the sample in the run's browser and measures it with the same probe
/// (<see cref="MotionState.ProbeFunction"/>). The sample always runs on WebAssembly and .NET 11, whatever
/// <c>BMOTION_E2E_HOST</c> and <c>BMOTION_E2E_FRAMEWORK</c> say, so only the run on their defaults tests it; every other
/// run reports it inconclusive, as it does the fixtures a host has nothing for.
/// </remarks>
[TestClass]
public sealed class LazyLoadingTests
{
    private const int DefaultTimeout = 15_000;

    // The card at rest: where its enter lands, and where the runtime takes a prerendered one over.
    private const string AtRest = "s => Math.abs(s.opacity - 1) < 0.01 && Math.abs(s.y) < 0.5";

    private LazyLoadingSample? _sample;
    private IBrowserContext? _context;

    private string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        if (E2EEnvironment.Host is not HarnessHostKinds.WebAssembly || E2EEnvironment.Framework is not "net11.0")
            Assert.Inconclusive($"The lazy loading sample runs on WebAssembly and net11.0 whatever the run drives, so the {HarnessHostKinds.WebAssembly} run on net11.0 tests it, not this {E2EEnvironment.Host} run on {E2EEnvironment.Framework}");

        string? unavailableReason = null;

        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Bmotion", "BMOTION", E2EEnvironment.Configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            unavailableReason = $"the lazy loading sample could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}";
        }

        if (unavailableReason is not null)
        {            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + unavailableReason);
            Assert.Inconclusive("The browser tests could not run: " + unavailableReason);
        }
    }

    [TestCleanup]
    public async Task CloseContextAsync()
    {
        if (_context is null) return;

        await _context.CloseAsync();
        _context = null;
    }

    [TestMethod]
    public async Task TheHomePage_NeverDownloadsBitBmotion_AndTheShowcaseDownloadsItOnce_BeforeItsCardEnters()
    {
        var page = await NewPageAsync();
        var requests = _sample!.Track(page);
        var problems = TrackProblems(page, BaseUrl);

        await GotoAsync(page, "/");
        await WaitForInteractiveAsync(page);
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the showcase renders and its card enters.
        await OpenTheShowcaseAsync(page);
        await AssertTheShowcaseWorksAsync(page);

        // Home and back: the router asks for the assembly again, and nothing is downloaded again. The layout's call finds
        // the services it created the first time, and the card enters anew.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await OpenTheShowcaseAsync(page);
        await AssertTheShowcaseWorksAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    [TestMethod]
    public async Task ADeepLinkToTheShowcase_IsPrerenderedWithItsEnterInCss_AndTakenOverWithoutPlayingItAgain()
    {
        var page = await NewPageAsync();
        var requests = _sample!.Track(page);
        var problems = TrackProblems(page, BaseUrl);

        // Every frame of the card, from the page the server sent through the runtime taking it over.
        await page.AddInitScriptAsync($$"""
            (() => {
                const probe = {{MotionState.ProbeFunction}};
                const samples = window.__bmCard = [];
                const sample = () => {
                    if (window.__bmStop) return;
                    const s = probe('#card');
                    if (s) samples.push({ opacity: s.opacity, y: s.y, interactive: document.querySelector('.page')?.dataset.interactive === 'true' });
                    requestAnimationFrame(sample);
                };
                requestAnimationFrame(sample);
            })();
            """);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - the layout, whose call checks its
        // options against the server's registration, and the card, whose enter it compiles to CSS (BmPrerenderMode.Animate).
        var response = await GotoAsync(page, "/showcase");
        var actualValue = response?.Status;
        Assert.AreEqual(200, actualValue);
        var prerendered = await response!.TextAsync();
        Assert.Contains("id=\"card\"", prerendered);
        Assert.Contains("--bm-", prerendered);

        // Program.cs downloaded the assembly before the app started, and the router rendered the same page at once.
        await WaitForInteractiveAsync(page);
        await AssertTheShowcaseWorksAsync(page);

        var samples = (await page.EvaluateAsync<JsonElement>("() => { window.__bmStop = true; return window.__bmCard; }"))
            .EnumerateArray()
            .Select(s => (Opacity: s.GetProperty("opacity").GetDouble(), Y: s.GetProperty("y").GetDouble(), Interactive: s.GetProperty("interactive").GetBoolean()))
            .ToArray();
        Assert.IsTrue(samples.Any(s => s.Interactive is false) && samples.Any(s => s.Interactive),
            $"The card was not sampled both as served and once interactive: {samples.Length} samples, {samples.Count(s => s.Interactive)} interactive.");

        // Never back towards its Initial state: the runtime took the card over at rest, or carried its CSS enter on, and
        // did not play the enter again - which it would, were the options or the prerendered state lost on the way.
        for (var i = 1; i < samples.Length; i++)
        {
            Assert.IsGreaterThanOrEqualTo(samples[i - 1].Opacity - 0.02, samples[i].Opacity, $"The card's opacity went back from {samples[i - 1].Opacity} to {samples[i].Opacity}.");
            Assert.IsLessThanOrEqualTo(samples[i - 1].Y + 1, samples[i].Y, $"The card's y went back from {samples[i - 1].Y} to {samples[i].Y}.");
        }
        await WaitForStateAsync(page, "#card", AtRest);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    /// <summary>
    /// Follows the link to the showcase and watches its card enter, through a frame on the way - an enter that snapped
    /// to its end would never show one - to rest.
    /// </summary>
    private async Task OpenTheShowcaseAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Showcase", Exact = true }).ClickAsync();
        await WaitForStateAsync(page, "#card", "s => s.opacity > 0.05 && s.opacity < 0.95");
        await page.WaitForURLAsync(BaseUrl + "/showcase");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Showcase");
        await WaitForStateAsync(page, "#card", AtRest);
    }

    /// <summary>
    /// What the showcase does once interactive: the card grows under the pointer and back, its listeners attached through
    /// the trimmed interop; the ball slides and the number counts up from code, through the service the page took from
    /// <c>GetBmotionService&lt;BmotionAnimateService&gt;()</c> - each through the values on the way, not straight to its end.
    /// </summary>
    private static async Task AssertTheShowcaseWorksAsync(IPage page)
    {
        await HoverUntilAsync(page, "#card", "s => s.scaleX > 1.01");
        await WaitForStateAsync(page, "#card", "s => Math.abs(s.scaleX - 1.1) < 0.01");
        await page.Mouse.MoveAsync(1, 1);
        await WaitForStateAsync(page, "#card", "s => Math.abs(s.scaleX - 1) < 0.01");

        // A Web Animation the service started on an element the page handed it by reference.
        await WaitForStateAsync(page, ".ball", "s => Math.abs(s.x) < 0.5");
        await page.Locator("#slide").ClickAsync();
        await WaitForStateAsync(page, ".ball", "s => s.x > 1 && s.x < 159");
        await WaitForStateAsync(page, ".ball", "s => Math.abs(s.x - 160) < 0.5");

        // A number on Bmotion's per-frame loop, which calls back into .NET every frame, rendered by the page each time.
        var count = page.Locator("#count");
        await Expect(count).ToHaveTextAsync("0");
        await count.ClickAsync();
        await page.WaitForFunctionAsync("() => { const n = Number(document.querySelector('#count').textContent); return n > 0 && n < 100; }",
            null, new() { Timeout = DefaultTimeout, PollingInterval = null });
        await Expect(count).ToHaveTextAsync("100");
    }

    /// <summary>
    /// Hovers the element, from well clear of it so pointerenter really fires, until its state satisfies
    /// <paramref name="predicate"/>. An element listens for the pointer once it has initialised, a moment after it
    /// rendered, and one the runtime took over at rest plays nothing that says when; so the pointer comes back until it
    /// answers.
    /// </summary>
    private static async Task HoverUntilAsync(IPage page, string selector, string predicate)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(DefaultTimeout);
        while (true)
        {
            await page.Mouse.MoveAsync(1, 1);
            await page.Locator(selector).HoverAsync();
            try
            {
                await WaitForStateAsync(page, selector, predicate, timeout: 1_000);
                return;
            }
            catch (AssertFailedException) when (DateTime.UtcNow < deadline)
            {
                // Not listening yet.
            }
        }
    }

    private async Task<IPage> NewPageAsync()
    {
        var browser = await PlaywrightSession.BrowserAsync();
        _context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            // The sample follows the visitor's preference (BmReducedMotionMode.User): explicit, so the OS of the machine
            // running the suite cannot turn off the motion measured here.
            ReducedMotion = ReducedMotion.NoPreference,
        });
        var page = await _context.NewPageAsync();
        page.SetDefaultTimeout(DefaultTimeout);
        return page;
    }

    // Generous, as the harness pages' are: a WebAssembly boot on a CI runner routinely takes longer than a click.
    private Task<IResponse?> GotoAsync(IPage page, string path) => page.GotoAsync(BaseUrl + path, new() { Timeout = 90_000 });

    /// <summary>The layout's marker, which turns true once the page runs in the browser.</summary>
    private static Task WaitForInteractiveAsync(IPage page) => page.WaitForFunctionAsync(
        "() => document.querySelector('.page')?.dataset.interactive === 'true'",
        null, new() { Timeout = 90_000 });

    /// <summary>
    /// Waits until the element's state satisfies <paramref name="predicate"/>, polling every animation frame, as
    /// <see cref="BmotionPageTest"/> does; fails with the last state.
    /// </summary>
    private static async Task WaitForStateAsync(IPage page, string selector, string predicate, int timeout = DefaultTimeout)
    {
        try
        {
            await page.WaitForFunctionAsync(
                $"sel => {{ const s = ({MotionState.ProbeFunction})(sel); return !!s && ({predicate})(s); }}",
                selector, new() { Timeout = timeout, PollingInterval = null });
        }
        catch (TimeoutException exception)
        {
            var last = await page.EvaluateAsync<JsonElement>($"sel => ({MotionState.ProbeFunction})(sel)", selector);
            throw new TimeoutException($"'{selector}' never satisfied `{predicate}` within {timeout} ms. Last state: {MotionState.From(last)?.ToString() ?? "(no element)"}", exception);
        }
    }

    /// <summary>
    /// Every console error, unhandled exception, failed request and error response of the sample the page reports -
    /// stricter than <see cref="BmotionPageTest"/>, which fails a test on the fatal ones only: an app that lazy loads
    /// must run clean.
    /// </summary>
    private static List<string> TrackProblems(IPage page, string origin)
    {
        var problems = new List<string>();
        void Add(string problem) { lock (problems) problems.Add(problem); }

        page.Console += (_, message) =>
        {
            if (message.Type == "error") Add($"console error: {message.Text}");
        };
        page.PageError += (_, error) => Add("unhandled error: " + error);
        page.RequestFailed += (_, request) =>
        {
            if (request.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"request failed: {request.Url} ({request.Failure})");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"HTTP {response.Status}: {response.Url}");
        };

        return problems;
    }

    private static void AssertNoProblems(List<string> problems)
    {
        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }
}
