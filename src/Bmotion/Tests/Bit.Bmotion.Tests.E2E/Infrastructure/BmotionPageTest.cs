using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every fixture: opens the fixture's harness page in a browser context of its own, waits until the page
/// is interactive, and fails the test on anything the page reported as fatal. Also the toolbox the tests measure
/// motion with - see <see cref="ProbeAsync"/>, <see cref="WaitForStateAsync"/> and <see cref="SampleAsync"/>.
/// </summary>
/// <remarks>
/// Manages Playwright itself rather than deriving from <c>Microsoft.Playwright.MSTest.PageTest</c>, because
/// runsettings are not threaded through the Microsoft.Testing.Platform runner reliably; the browser is configured
/// from <c>BMOTION_E2E_*</c> environment variables instead (see <see cref="BrowserLaunch"/>).
/// <br/>
/// The fatal-error check is load-bearing. An unhandled exception in a Server circuit terminates the circuit, and in
/// WebAssembly it takes the renderer down; either way every later interaction is silently dropped and the test only
/// ever sees an event that never arrives. An exception thrown by the bridge from inside a browser event handler is
/// quieter still - nothing but the console ever hears of it. So every uncaught page error fails the test, quoting
/// the console message that reported the actual cause.
/// </remarks>
public abstract class BmotionPageTest
{
    // What the Blazor runtimes and the bridge write to the console when something went wrong for real: WebAssembly's
    // renderer logs "Unhandled exception rendering component", blazor.web.js reports a circuit an exception
    // terminated, and the bridge prefixes its own errors with "bmotion:".
    private static readonly string[] FatalConsoleFragments =
    [
        "Unhandled exception rendering component",
        "unhandled exception on the current circuit",
        "Circuit has closed due to an error",
        "bmotion:",
    ];

    protected const int DefaultTimeout = 15_000;

    private readonly object _errorsLock = new();
    private readonly List<string> _fatalErrors = [];
    private IBrowserContext? _context;

    protected IPage Page { get; private set; } = default!;

    protected WebHarnessHost Host { get; private set; } = default!;

    /// <summary>The harness route a fixture drives, e.g. "basics".</summary>
    protected abstract string Route { get; }

    /// <summary>The library options the fixture's host runs with.</summary>
    protected virtual HarnessProfile Profile => HarnessProfile.Default;

    /// <summary>A render mode for this fixture's pages other than the run's (<c>?mode=</c>), e.g. "ssr".</summary>
    protected virtual string? RenderMode => null;

    /// <summary>Whether to wait for <c>#status</c> to read "ready" - false for pages that never become interactive.</summary>
    protected virtual bool WaitForInteractive => RenderMode is not HarnessHostKinds.Ssr;

    /// <summary>Substrings of page errors a fixture provokes on purpose; anything else still fails the test.</summary>
    protected virtual IReadOnlyList<string> ExpectedErrors => [];

    /// <summary>The render mode every page of the run is served in.</summary>
    protected static string RunHost => E2EEnvironment.Host;

    /// <summary>
    /// Whether the runtime the page landed on has Bit.Bmotion's per-frame loop (WebAssembly). Without it (Blazor
    /// Server) only compositor-eligible animations play, and everything else lands on its target at once - the
    /// tests assert both halves of that contract. Auto may land on either, so this is read off the page.
    /// </summary>
    protected bool FrameLoop { get; private set; }

    [TestInitialize]
    public async Task SetUpAsync()
    {
        Host = await HarnessTarget.HostAsync(Profile);

        var browser = await PlaywrightSession.BrowserAsync();
        var options = new BrowserNewContextOptions
        {
            BaseURL = Host.BaseUrl,
            ViewportSize = new() { Width = 1280, Height = 720 },
            // Explicit, so the OS of the machine running the suite can't change what "the user's preference" is.
            ReducedMotion = ReducedMotion.NoPreference,
        };
        ConfigureContext(options);
        _context = await browser.NewContextAsync(options);
        Page = await _context.NewPageAsync();
        Page.SetDefaultTimeout(DefaultTimeout);
        Page.Console += OnConsole;
        Page.PageError += OnPageError;

        await OpenAsync(Route);
    }

    [TestCleanup]
    public async Task TearDownAsync()
    {
        if (_context is not null)
        {
            Page.Console -= OnConsole;
            Page.PageError -= OnPageError;
            await _context.CloseAsync();
            _context = null;
        }

        string[] fatal;
        lock (_errorsLock) fatal = [.. _fatalErrors];

        if (fatal.Length > 0)
            Assert.Fail($"The page reported {fatal.Length} fatal error(s) on {RunHost}:{Environment.NewLine}{string.Join(Environment.NewLine, fatal)}" +
                        $"{Environment.NewLine}Recent host output:{Environment.NewLine}{Host.RecentOutput}");
    }

    /// <summary>Lets a fixture adjust its browser context: reduced motion, scripting, viewport.</summary>
    protected virtual void ConfigureContext(BrowserNewContextOptions options) { }

    /// <summary>Navigates to a harness route (with an optional query) and waits for it as the fixture is configured to.</summary>
    protected async Task OpenAsync(string route, string? query = null)
    {
        var parameters = new List<string>();
        if (RenderMode is { } mode) parameters.Add($"mode={mode}");
        if (string.IsNullOrEmpty(query) is false) parameters.Add(query);
        var url = $"{Host.BaseUrl}/{route}{(parameters.Count > 0 ? "?" + string.Join("&", parameters) : "")}";

        // Generous: a Debug WebAssembly boot, or a prerender followed by a circuit handshake, routinely takes longer
        // than a click on a CI runner without anything being wrong.
        await Page.GotoAsync(url, new() { Timeout = 90_000 });

        if (WaitForInteractive is false) return;

        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("ready", new() { Timeout = 90_000 });
        FrameLoop = await Page.Locator("#caps").GetAttributeAsync("data-frame-loop") == "true";
    }

    // ── Events ───────────────────────────────────────────────────────────────────

    /// <summary>The log entries a page recorded for <paramref name="name"/>.</summary>
    protected ILocator Event(string name) => Page.Locator($"#log li[data-event='{name}']");

    /// <summary>Waits until <paramref name="name"/> was logged exactly <paramref name="count"/> times.</summary>
    protected async Task ExpectEventAsync(string name, int count = 1, int timeout = DefaultTimeout)
    {
        try
        {
            await Assertions.Expect(Event(name)).ToHaveCountAsync(count, new() { Timeout = timeout });
        }
        catch (PlaywrightException)
        {
            var actual = await Event(name).CountAsync();
            throw new AssertFailedException($"Expected '{name}' {count} time(s) within {timeout} ms on {RunHost}, saw it {actual} time(s). " +
                                            $"The log: [{string.Join(", ", await LoggedAsync())}]");
        }
    }

    /// <summary>The index of <paramref name="name"/> in the event log, or -1.</summary>
    protected async Task<int> EventIndexAsync(string name) => Array.IndexOf(await LoggedAsync(), name);

    /// <summary>Every logged event, in the order they fired.</summary>
    protected Task<string[]> LoggedAsync() => Page.Locator("#log li").EvaluateAllAsync<string[]>("els => els.map(e => e.dataset.event)");

    // ── Motion ───────────────────────────────────────────────────────────────────

    /// <summary>What the element matching <paramref name="selector"/> looks like right now.</summary>
    protected async Task<MotionState> ProbeAsync(string selector)
    {
        var json = await Page.EvaluateAsync<JsonElement>($"sel => ({MotionState.ProbeFunction})(sel)", selector);
        return MotionState.From(json) ?? throw new AssertFailedException($"No element matches '{selector}'.");
    }

    /// <summary>
    /// Waits until the element's state satisfies <paramref name="predicate"/> - a JavaScript function of the probed
    /// state (<c>s =&gt; Math.abs(s.x - 120) &lt; 1</c>) - polling every animation frame. Fails with the last state.
    /// </summary>
    protected async Task WaitForStateAsync(string selector, string predicate, int timeout = DefaultTimeout)
    {
        try
        {
            await Page.WaitForFunctionAsync(
                $"sel => {{ const s = ({MotionState.ProbeFunction})(sel); return !!s && ({predicate})(s); }}",
                selector, new() { Timeout = timeout, PollingInterval = null });
        }
        catch (TimeoutException)
        {
            var last = await Page.EvaluateAsync<JsonElement>($"sel => ({MotionState.ProbeFunction})(sel)", selector);
            throw new AssertFailedException($"'{selector}' never satisfied `{predicate}` within {timeout} ms on {RunHost}. Last state: {MotionState.From(last)?.ToString() ?? "(no element)"}");
        }
    }

    /// <summary>Samples the element once per animation frame for <paramref name="milliseconds"/>, each sample stamped with its time.</summary>
    protected async Task<IReadOnlyList<MotionState>> SampleAsync(string selector, int milliseconds)
    {
        var samples = await Page.EvaluateAsync<JsonElement>($$"""
            async ([sel, ms]) => {
                const probe = {{MotionState.ProbeFunction}};
                const out = [];
                const start = performance.now();
                const end = start + ms;
                while (performance.now() < end) {
                    const s = probe(sel);
                    if (s) s.t = performance.now() - start;
                    out.push(s);
                    await new Promise(r => requestAnimationFrame(r));
                }
                return out;
            }
            """, new object[] { selector, milliseconds });

        return [.. samples.EnumerateArray().Select(MotionState.From).OfType<MotionState>()];
    }

    /// <summary>
    /// Waits until the element stops moving - the same state on two probes <paramref name="quietMs"/> apart - and
    /// returns that state.
    /// </summary>
    protected async Task<MotionState> WaitForRestAsync(string selector, int quietMs = 250, int timeout = DefaultTimeout)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        var previous = await ProbeAsync(selector);
        while (DateTime.UtcNow < deadline)
        {
            await Page.WaitForTimeoutAsync(quietMs);
            var current = await ProbeAsync(selector);
            if (Math.Abs(current.X - previous.X) < 0.1 && Math.Abs(current.Y - previous.Y) < 0.1 &&
                Math.Abs(current.ScaleX - previous.ScaleX) < 0.001 && Math.Abs(current.Opacity - previous.Opacity) < 0.001 &&
                Math.Abs(current.Left - previous.Left) < 0.1 && Math.Abs(current.Width - previous.Width) < 0.1)
                return current;
            previous = current;
        }

        throw new AssertFailedException($"'{selector}' never came to rest within {timeout} ms on {RunHost}. Last state: {previous}");
    }

    /// <summary>
    /// Whether x jumped from 0 to <paramref name="target"/> instead of animating: every frame at one end or the
    /// other - however long the change took to land - and the last at the target.
    /// </summary>
    protected static bool Snapped(IReadOnlyList<MotionState> samples, double target)
        => samples.Count > 0
           && samples.All(s => Math.Abs(s.X) < 0.5 || Math.Abs(s.X - target) < 0.5)
           && Math.Abs(samples[^1].X - target) < 0.5;

    /// <summary>Parses a number the page wrote (CSS values, readouts), whatever the culture of the machine running the suite.</summary>
    protected static double Number(string text) => double.Parse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Clicks the control with id <paramref name="id"/>.</summary>
    protected Task ClickAsync(string id) => Page.Locator($"#{id}").ClickAsync();

    /// <summary>The text of an <c>&lt;output id&gt;</c> readout.</summary>
    protected async Task<string> ReadoutAsync(string id) => (await Page.Locator($"output#{id}").TextContentAsync())?.Trim() ?? "";

    /// <summary>Waits until a readout shows exactly <paramref name="expected"/>.</summary>
    protected Task ExpectReadoutAsync(string id, string expected, int timeout = DefaultTimeout)
        => Assertions.Expect(Page.Locator($"output#{id}")).ToHaveTextAsync(expected, new() { Timeout = timeout });

    /// <summary>Waits until a numeric readout satisfies <paramref name="condition"/>; returns the value it settled on.</summary>
    protected async Task<double> WaitForReadoutAsync(string id, Func<double, bool> condition, int timeout = DefaultTimeout)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        string text;
        do
        {
            text = await ReadoutAsync(id);
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) && condition(value))
                return value;
            await Task.Delay(50);
        } while (DateTime.UtcNow < deadline);

        throw new AssertFailedException($"Readout '{id}' never met its condition within {timeout} ms on {RunHost}; last value '{text}'.");
    }

    // ── Pointer ──────────────────────────────────────────────────────────────────

    /// <summary>Hovers the element's centre after moving the pointer well clear of it, so pointerenter really fires.</summary>
    protected async Task HoverAsync(string selector)
    {
        await Page.Mouse.MoveAsync(1, 1);
        await Page.Locator(selector).HoverAsync();
    }

    /// <summary>The element's box in viewport coordinates, scrolled into view first so the pointer can reach it.</summary>
    protected async Task<LocatorBoundingBoxResult> BoxAsync(string selector)
    {
        await Page.Locator(selector).ScrollIntoViewIfNeededAsync();
        return await Page.Locator(selector).BoundingBoxAsync() ?? throw new AssertFailedException($"'{selector}' has no box.");
    }

    /// <summary>
    /// Waits until the bridge has attached drag listeners to the element - it gives the element a touch-action of
    /// its own as it does - for draggable elements a test can't otherwise hear arming (a reorder group's items).
    /// </summary>
    protected Task WaitForDragReadyAsync(string selector)
        => Assertions.Expect(Page.Locator(selector)).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("touch-action"));

    /// <summary>The centre of the element's box, in viewport coordinates (scrolled into view first).</summary>
    protected async Task<(double X, double Y)> CentreAsync(string selector)
    {
        var box = await BoxAsync(selector);
        return (box.X + (box.Width / 2), box.Y + (box.Height / 2));
    }

    /// <summary>A pointer drag from the element's centre by (dx, dy), in <paramref name="steps"/> moves.</summary>
    protected async Task DragByAsync(string selector, double dx, double dy, int steps = 12, bool release = true)
    {
        var (x, y) = await CentreAsync(selector);
        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        for (var i = 1; i <= steps; i++)
            await Page.Mouse.MoveAsync((float)(x + (dx * i / steps)), (float)(y + (dy * i / steps)));
        if (release) await Page.Mouse.UpAsync();
    }

    private void OnConsole(object? sender, IConsoleMessage message)
    {
        if (message.Type is not "error") return;
        if (FatalConsoleFragments.Any(fragment => message.Text.Contains(fragment, StringComparison.OrdinalIgnoreCase)) is false) return;
        if (ExpectedErrors.Any(expected => message.Text.Contains(expected, StringComparison.OrdinalIgnoreCase))) return;

        lock (_errorsLock) _fatalErrors.Add($"console: {message.Text}");
    }

    private void OnPageError(object? sender, string error)
    {
        if (ExpectedErrors.Any(expected => error.Contains(expected, StringComparison.OrdinalIgnoreCase))) return;

        lock (_errorsLock) _fatalErrors.Add($"uncaught: {error}");
    }
}
