using System.Globalization;
using Microsoft.Playwright;

namespace ButilTests.Benchmarks;

/// <summary>
/// The runtime half of the suite: what a call through Bit.Butil costs inside a real browser.
/// </summary>
/// <remarks>
/// Driven against the <c>/benchmark</c> harness page in <c>Bit.Butil.Samples.Core</c>. The
/// measurements are taken <em>in .NET, on the page</em> rather than out here, because the round trip
/// is what is being measured and doing the timing from Playwright would add the automation channel
/// to every figure. This class starts the browser, presses the buttons, parses the lines the page
/// emits, and holds them against <see cref="Budgets"/>.
/// <br/>
/// The browser is launched the same way the E2E suite launches its own, through the same environment
/// variables (<c>BUTIL_E2E_CHANNEL</c>, <c>BUTIL_E2E_EXECUTABLE</c>, <c>BUTIL_E2E_HEADED</c>), so one
/// setup serves both suites.
/// </remarks>
internal static class InteropBenchmarks
{
    internal static async Task Run(Report report, string baseUrl)
    {
        using var playwright = await Playwright.CreateAsync();

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("BUTIL_E2E_HEADED") != "1"
        };

        var channel = Environment.GetEnvironmentVariable("BUTIL_E2E_CHANNEL");
        if (string.IsNullOrWhiteSpace(channel) is false) launchOptions.Channel = channel;

        var executable = Environment.GetEnvironmentVariable("BUTIL_E2E_EXECUTABLE");
        if (string.IsNullOrWhiteSpace(executable) is false) launchOptions.ExecutablePath = executable;

        await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);
        await using var context = await browser.NewContextAsync(new()
        {
            BaseURL = baseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new() { Width = 1280, Height = 720 },
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("/benchmark", new() { Timeout = 120_000 });
        await Assertions.Expect(page.Locator("#status")).ToHaveTextAsync("ready", new() { Timeout = 120_000 });

        await RunTimings(report, page);
        await RunRateLimiting(report, page);

        // A second page, because the mode is chosen at startup: the sample app reads ?lazy=1 off its
        // start URL and calls BitButil.UseLazyScripts(). A fresh page is the only way to get a run of
        // the same benchmark with the loader in front of every call.
        var lazyPage = await context.NewPageAsync();
        await lazyPage.GotoAsync("/benchmark?lazy=1", new() { Timeout = 120_000 });
        await Assertions.Expect(lazyPage.Locator("#status")).ToHaveTextAsync("ready", new() { Timeout = 120_000 });

        await RunLazyScripts(report, lazyPage);
    }

    /// <summary>
    /// What the lazy-scripts loader costs once the module it guards is already loaded.
    /// </summary>
    /// <remarks>
    /// Under <c>UseLazyScripts</c> every interop call goes through <c>ButilScriptLoader.EnsureLoaded</c>
    /// first. That is unavoidable for the call that triggers the import, and it is the point of the
    /// feature - but it also sits in front of the millionth call to an already-loaded module, where it
    /// must cost as close to nothing as a dictionary lookup can. Measured as the ratio against the same
    /// benchmark in bundle mode, on the same machine in the same run, because an absolute figure here
    /// would be measuring the browser.
    /// <br/>
    /// Reported rather than budgeted: the two runs are separate pages with separate warm-ups, so the
    /// ratio carries more noise than the gate ratios do. It is here to be watched - a loader that
    /// started doing real work per call would show up as a number well above one.
    /// </remarks>
    private static async Task RunLazyScripts(Report report, IPage page)
    {
        Report.Section("Lazy scripts (the loader in front of an already-loaded module)");

        await page.Locator("#bench-invoke-value").ClickAsync();
        await Assertions.Expect(page.Locator("#status")).ToHaveTextAsync("done:invoke-value", new() { Timeout = 120_000 });

        var results = await ParseResults(page);
        if (results.TryGetValue("invoke-value", out var lazy) is false)
        {
            report.Fail("the lazy-scripts page did not report 'invoke-value'.");
            return;
        }

        Report.Info("value call, lazy scripts", lazy.PerOpUs, "us");
        Report.Info("vs the same call in bundle mode", lazy.PerOpUs / Math.Max(BundleModeValueCallUs, 0.001), "x");
    }

    // Kept from the bundle-mode run so the lazy figure has something to be a ratio of.
    private static double BundleModeValueCallUs;

    private static async Task RunTimings(Report report, IPage page)
    {
        Report.Section("Interop round trips (in-browser, measured from .NET)");

        await page.Locator("#bench-all").ClickAsync();
        // Matched on the prefix so a benchmark that threw ends the wait with "error:all" rather than
        // burning the whole timeout; the reason itself comes back on the page's error line.
        await Assertions.Expect(page.Locator("#status")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^(done|error):all$"),
                                                                        new() { Timeout = 180_000 });

        var results = await ParseResults(page);

        if (results.TryGetValue("error", out var error))
        {
            report.Fail($"the harness page threw: {error.Extra.GetValueOrDefault("message", "(no message)")}");
            return;
        }

        Measure(report, results, "invoke-value", Budgets.MaxValueCallUs, "value call (result deserialized)");
        BundleModeValueCallUs = results["invoke-value"].PerOpUs;
        Measure(report, results, "invoke-void", Budgets.MaxVoidCallUs, "void call");
        Measure(report, results, "element-read", Budgets.MaxElementReadUs, "ElementReference read");

        // Reported rather than budgeted. The Dom handle path is deliberately two round trips per
        // iteration, so a ceiling on it would only restate the two above.
        if (results.TryGetValue("dom-handle", out var domHandle))
            Report.Info("Dom query + read (2 round trips)", domHandle.PerOpUs, "us");

        // The in-process fast path only exists under WebAssembly, and the page reports a no-op
        // everywhere else. Asserted as a ratio rather than a ceiling: what makes it worth having is
        // that it beats the async path on the same machine in the same run.
        if (results.TryGetValue("invoke-fast", out var fast) && results.TryGetValue("invoke-value", out var value))
        {
            Report.Info("fast (in-process) call", fast.PerOpUs, "us");
            report.AtLeast("fast call speed-up over async", value.PerOpUs / Math.Max(fast.PerOpUs, 0.001), 1, "x");
        }

        foreach (var (name, measurement) in results.Where(entry => entry.Key.StartsWith("payload-", StringComparison.Ordinal)))
        {
            if (name.EndsWith("-MISMATCH", StringComparison.Ordinal))
            {
                report.Fail($"{name}: the payload came back the wrong length.");
                continue;
            }

            var bytes = measurement.Extra.TryGetValue("bytes", out var raw) ? double.Parse(raw, CultureInfo.InvariantCulture) : 0;
            if (bytes <= 0) continue;

            var mbPerSecond = bytes / (measurement.PerOpUs / 1_000_000) / (1024 * 1024);
            if (bytes >= 1024 * 1024)
                report.AtLeast($"payload throughput ({bytes / 1024 / 1024:0} MB)", mbPerSecond, Budgets.MinPayloadThroughputMbPerSecond, "MB/s");
            else
                Report.Info($"payload throughput ({bytes / 1024:0} KB)", mbPerSecond, "MB/s");
        }
    }

    /// <summary>
    /// Proves the rate limit does what it claims, on the same machine in the same run: an identical
    /// burst of events is fired at an ungated subscription and at a gated one, and the two delivery
    /// counts are compared.
    /// </summary>
    /// <remarks>
    /// A ratio rather than a count, because how many events a browser can dispatch in a burst is a
    /// property of the machine - but how many of them survive a 50 ms gate compared with none at all
    /// is a property of the gate.
    /// </remarks>
    private static async Task RunRateLimiting(Report report, IPage page)
    {
        Report.Section("Rate limiting (the same burst, gated and ungated)");

        const int burst = 300;

        var ungatedMoves = await BurstMoves(page, "#gate-move-ungated", burst);
        var gatedMoves = await BurstMoves(page, "#gate-move-gated", burst);

        Report.Info($"mousemove, ungated ({burst} events)", ungatedMoves, "deliveries");
        Report.Info($"mousemove, 50 ms gate ({burst} events)", gatedMoves, "deliveries");
        report.AtLeast("mousemove traffic reduction", ungatedMoves / Math.Max(gatedMoves, 1d), Budgets.MinEventGateReduction, "x");
        report.AtLeast("mousemove deliveries while gated", gatedMoves, Budgets.MinGatedDeliveries, "");

        var ungatedResizes = await BurstResizes(page, "#gate-resize-ungated", burst);
        var gatedResizes = await BurstResizes(page, "#gate-resize-gated", burst);

        Report.Info("resize, ungated", ungatedResizes, "deliveries");
        Report.Info("resize, 50 ms gate", gatedResizes, "deliveries");
        // Against the observer floor, not the event one: an ungated ResizeObserver already delivers
        // at most once a frame, so the ratio here is capped by the gate divided by the frame time.
        report.AtLeast("resize traffic reduction", ungatedResizes / Math.Max(gatedResizes, 1d), Budgets.MinObserverGateReduction, "x");
        report.AtLeast("resize deliveries while gated", gatedResizes, Budgets.MinGatedDeliveries, "");
    }

    private static async Task<int> BurstMoves(IPage page, string startButton, int count)
    {
        await page.Locator(startButton).ClickAsync();
        await Assertions.Expect(page.Locator("#status")).ToContainTextAsync("ready:", new() { Timeout = 30_000 });

        // Dispatched synthetically rather than through Playwright's mouse, which coalesces moves the
        // way a real pointer does - the burst has to be a known number of events for the ratio to
        // mean anything.
        await page.EvaluateAsync(@"count => {
            const target = document.getElementById('gate-target');
            for (let i = 0; i < count; i++) {
                target.dispatchEvent(new MouseEvent('mousemove', {
                    bubbles: true, clientX: i % 100, clientY: i % 50
                }));
            }
        }", count);

        return await Drain(page);
    }

    private static async Task<int> BurstResizes(IPage page, string startButton, int count)
    {
        await page.Locator(startButton).ClickAsync();
        await Assertions.Expect(page.Locator("#status")).ToContainTextAsync("ready:", new() { Timeout = 30_000 });

        // A ResizeObserver delivers at most once a frame no matter how often the box changes within
        // one, so the resizes are spread across frames - otherwise the ungated half would report a
        // handful of deliveries and there would be nothing for the gate to reduce.
        await page.EvaluateAsync(@"count => new Promise(resolve => {
            const target = document.getElementById('gate-target');
            let i = 0;
            const step = () => {
                target.style.width = (100 + (i % 60)) + 'px';
                if (++i >= count) { target.style.width = '100px'; resolve(); return; }
                requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
        })", Math.Min(count, 120));

        return await Drain(page);
    }

    /// <summary>
    /// Waits out the gate's trailing send before reading the count, so a gated run is never
    /// measured mid-interval with its last delivery still pending.
    /// </summary>
    private static async Task<int> Drain(IPage page)
    {
        await Task.Delay(500);

        await page.Locator("#gate-report").ClickAsync();
        await Assertions.Expect(page.Locator("#status")).ToContainTextAsync("done:gate-", new() { Timeout = 30_000 });

        var results = await ParseResults(page);
        var line = results.LastOrDefault(entry => entry.Key.StartsWith("gate-", StringComparison.Ordinal));

        return line.Value is not null && line.Value.Extra.TryGetValue("delivered", out var delivered)
            ? int.Parse(delivered, CultureInfo.InvariantCulture)
            : 0;
    }

    private sealed record Measurement(double PerOpUs, Dictionary<string, string> Extra);

    /// <summary>
    /// Parses the harness page's output: one measurement per line, <c>name|key=value|key=value</c>.
    /// </summary>
    private static async Task<Dictionary<string, Measurement>> ParseResults(IPage page)
    {
        var text = await page.Locator("#results").TextContentAsync() ?? string.Empty;
        var results = new Dictionary<string, Measurement>(StringComparer.Ordinal);

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split('|');
            if (parts.Length < 2) continue;

            var extra = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var part in parts.Skip(1))
            {
                var pair = part.Split('=', 2);
                if (pair.Length == 2) extra[pair[0]] = pair[1];
            }

            var perOpUs = extra.TryGetValue("perOpUs", out var raw)
                ? double.Parse(raw, CultureInfo.InvariantCulture)
                : 0;

            results[parts[0]] = new Measurement(perOpUs, extra);
        }

        return results;
    }

    private static void Measure(Report report, Dictionary<string, Measurement> results, string key, double budget, string label)
    {
        if (results.TryGetValue(key, out var measurement) is false)
        {
            report.Fail($"the harness page did not report '{key}'.");
            return;
        }

        report.AtMost(label, measurement.PerOpUs, budget, "us");
    }
}
