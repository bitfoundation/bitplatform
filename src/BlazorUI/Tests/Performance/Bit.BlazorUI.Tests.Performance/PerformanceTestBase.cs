using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Base class for Playwright-based performance tests.
/// Manages the test host application lifecycle, and the browser with a fresh page for every test.
/// </summary>
/// <remarks>
/// The browser is picked from environment variables, which the Microsoft.Testing.Platform runner passes through
/// where it does not pass runsettings: <c>BROWSER</c> (<c>chromium</c> by default, <c>edge</c>, <c>firefox</c> or
/// <c>webkit</c>) and <c>HEADED=1</c> to watch the run.
/// <c>TEST_HOST_URL</c> moves the test host off <c>http://localhost:5280</c>, for a machine where another checkout's
/// host already holds that port - a host found answering there is reused rather than started.
/// <c>PERF_RENDER_MODE</c> picks where the performance pages run (<see cref="RenderMode"/>).
/// </remarks>
public abstract class PerformanceTestBase
{
    private static Process? _hostProcess;
    private static readonly object _lock = new();
    private static bool _isHostStarted;
    private static readonly HttpClient _sharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly SemaphoreSlim _browserLock = new(1, 1);
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private IBrowserContext? _context;

    protected static readonly string BaseUrl = Environment.GetEnvironmentVariable("TEST_HOST_URL") is { Length: > 0 } url
                                                    ? url.TrimEnd('/')
                                                    : "http://localhost:5280";
    protected const int DefaultTimeout = 30000;

    internal static string HostUrl => BaseUrl;

    /// <summary>The BROWSER the run asked for (Edge is a channel of Playwright's chromium): <c>chromium</c> when it asked for none.</summary>
    internal static string BrowserName => Environment.GetEnvironmentVariable("BROWSER") is { Length: > 0 } name ? name.ToLowerInvariant() : "chromium";

    /// <summary>
    /// Where the performance pages run, as <c>PERF_RENDER_MODE</c> names it: <c>server</c> (InteractiveServer, the default)
    /// or <c>wasm</c> (InteractiveWebAssembly). The host reads it off a cookie every browser context of the run carries; the
    /// other pages of the host run on the server whatever it says. Any other value fails every test before the host or the
    /// browser is started, rather than measuring the server under a name that asked for something else.
    /// </summary>
    internal static string RenderMode => Environment.GetEnvironmentVariable("PERF_RENDER_MODE") is { Length: > 0 } mode
        ? mode.ToLowerInvariant() switch
        {
            "server" => "server",
            "wasm" or "webassembly" => "wasm",
            _ => throw new InvalidOperationException($"Unknown PERF_RENDER_MODE '{mode}'. Use server or wasm.")
        }
        : "server";

    protected IPage Page { get; private set; } = null!;

    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// The browser context a test class needs, for the ones that cannot be driven in the default desktop one -
    /// a test of behaviour that only runs on a touch device, or at a phone's viewport. Null takes the defaults.
    /// </summary>
    protected virtual BrowserNewContextOptions? ContextOptions => null;

    /// <summary>
    /// Performance thresholds in milliseconds.
    /// </summary>
    protected static class Thresholds
    {
        // Initial render thresholds (ms)
        public const double Render10Components = 500;
        public const double Render100Components = 1000;
        public const double Render500Components = 3000;
        public const double Render1000Components = 5000;

        // Re-render thresholds (ms)
        public const double ReRender10Components = 200;
        public const double ReRender100Components = 500;
        public const double ReRender500Components = 1500;
        public const double ReRender1000Components = 3000;

        // Memory thresholds (MB)
        public const double Memory10Components = 50;
        public const double Memory100Components = 100;
        public const double Memory500Components = 200;
        public const double Memory1000Components = 400;
    }

    [TestInitialize]
    public async Task TestInitializeBase()
    {
        // Read first, so a PERF_RENDER_MODE the run does not know fails here and starts neither the host nor the browser.
        var renderMode = RenderMode;

        // The host and the browser are started by the first test and kept for the whole run (BrowserTestRun stops
        // them once it is over). Stopping them whenever no test happened to be running - between any two tests of a
        // sequential run - had every test start its own `dotnet run` and browser, racing the port of the last one.
        lock (_lock)
        {
            if (!_isHostStarted)
            {
                StartTestHost();
                _isHostStarted = true;
            }
        }

        await _browserLock.WaitAsync();
        try
        {
            _browser ??= await LaunchBrowser();
        }
        finally
        {
            _browserLock.Release();
        }

        _context = await _browser.NewContextAsync(ContextOptions);
        await _context.AddCookiesAsync([new Cookie { Name = "perf-render-mode", Value = renderMode, Url = BaseUrl }]);
        Page = await _context.NewPageAsync();

        // Wait for page to be ready.
        // NOTE: Do NOT use WaitForLoadStateAsync(NetworkIdle) here - Blazor Server keeps a
        // persistent SignalR WebSocket open, which Playwright counts as an active connection
        // and therefore NetworkIdle never fires. GotoAsync already waits for the Load event.
        await Page.GotoAsync(BaseUrl);
    }

    [TestCleanup]
    public async Task TestCleanupBase()
    {
        if (_context is not null)
        {
            try
            {
                await _context.CloseAsync();
            }
            catch (PlaywrightException)
            {
                // Firefox's driver can fail to tear down a context that opened popups ("removeBrowserContext ...
                // _maybeDontRestoreTabs"), and a browser left in that state fails to close every context after
                // it. The test itself has already passed or failed on its own, so the browser is dropped and the
                // next test launches a fresh one instead of inheriting the failure.
                await DiscardBrowser();
            }
            finally
            {
                _context = null;
            }
        }
    }

    /// <summary>
    /// Closes the browser and stops the test host the run started, once every test of it is done.
    /// </summary>
    internal static async Task ShutdownAsync()
    {
        await _browserLock.WaitAsync();
        try
        {
            if (_browser is not null) await _browser.CloseAsync();
        }
        catch (PlaywrightException)
        {
            // already unusable; the driver reaps it with the Playwright instance
        }
        finally
        {
            _playwright?.Dispose();
            _browser = null;
            _playwright = null;
            _browserLock.Release();
        }

        lock (_lock)
        {
            StopTestHost();
        }
    }

    private static async Task DiscardBrowser()
    {
        await _browserLock.WaitAsync();
        try
        {
            if (_browser is not null) await _browser.CloseAsync();
        }
        catch (PlaywrightException)
        {
            // already unusable; the driver reaps it with the Playwright instance
        }
        finally
        {
            _browser = null;
            _browserLock.Release();
        }
    }

    private static async Task<IBrowser> LaunchBrowser()
    {
        // A launch that failed leaves the driver behind; the next test reuses it instead of starting
        // another one that only the cleanup of the last test would dispose.
        _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

        SetDefaultExpectTimeout(DefaultTimeout);

        var browserName = Environment.GetEnvironmentVariable("BROWSER");
        var options = new BrowserTypeLaunchOptions { Headless = Environment.GetEnvironmentVariable("HEADED") != "1" };

        return browserName?.ToLowerInvariant() switch
        {
            null or "" or "chromium" => await _playwright.Chromium.LaunchAsync(new(options)
            {
                // performance.memory only reports real numbers with this flag.
                Args = ["--enable-precise-memory-info"]
            }),
            // The installed Edge, for a machine that has no Playwright-managed Chromium downloaded.
            "edge" or "msedge" => await _playwright.Chromium.LaunchAsync(new(options)
            {
                Channel = "msedge",
                Args = ["--enable-precise-memory-info"]
            }),
            "firefox" => await _playwright.Firefox.LaunchAsync(options),
            "webkit" => await _playwright.Webkit.LaunchAsync(options),
            _ => throw new InvalidOperationException($"Unknown BROWSER '{browserName}'. Use chromium, edge, firefox or webkit.")
        };
    }

    private static void StartTestHost()
    {
        // A host already serving the URL is used as it is. It lets a run attach to one started by hand -
        // in another configuration, or left running between runs so the implicit build is paid once - and
        // it is what keeps a second run from launching a host that could only fail to take the port.
        if (IsTestHostResponding()) return;

        var testHostPath = GetTestHostPath();

        // The test host project targets multiple frameworks (net10.0/net11.0).
        // 'dotnet run' refuses to pick one automatically and exits immediately, which
        // previously caused all browser tests to fail with a misleading 30-second timeout.
        // Run the host on the same TFM as the test runner so the right one is used.
        var tfm = $"net{Environment.Version.Major}.{Environment.Version.Minor}";

        // And in the configuration the tests were built in: a Release build of the tests measures a Release host, the only
        // kind whose numbers are a baseline.
        var configuration = typeof(PerformanceTestBase).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{testHostPath}\" --framework {tfm} --configuration {configuration} --urls {BaseUrl}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _hostProcess = Process.Start(startInfo);

        // The host's output is read as it is written, for as long as it runs. A redirected stream nobody reads fills
        // its pipe, the console logger then blocks on writing, and every request of the host blocks on logging - so a
        // host kept for the whole run stops answering after a few hundred requests. Only the tail is kept, for the
        // message below.
        var hostOutput = new HostOutputTail();
        _hostProcess!.OutputDataReceived += (_, e) => hostOutput.Add(e.Data);
        _hostProcess.ErrorDataReceived += (_, e) => hostOutput.Add(e.Data);
        _hostProcess.BeginOutputReadLine();
        _hostProcess.BeginErrorReadLine();

        // 'dotnet run' performs an implicit build on first invocation, which can take
        // longer than 30s on a clean machine or in CI, so allow up to 2 minutes.
        var maxWait = TimeSpan.FromSeconds(120);
        var waited = TimeSpan.Zero;
        var interval = TimeSpan.FromMilliseconds(500);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        while (waited < maxWait)
        {
            if (_hostProcess is not null && _hostProcess.HasExited)
            {
                _hostProcess.WaitForExit(); // flushes the asynchronous readers
                throw new InvalidOperationException(
                    $"Test host process exited before it could serve requests. " +
                    $"ExitCode={_hostProcess.ExitCode}.{Environment.NewLine}" +
                    $"OUTPUT:{Environment.NewLine}{hostOutput}");
            }

            try
            {
                var response = httpClient.GetAsync(BaseUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Host not ready yet
            }

            Thread.Sleep(interval);
            waited += interval;
        }

        throw new InvalidOperationException($"Test host failed to start within {maxWait.TotalSeconds} seconds");
    }

    private static void StopTestHost()
    {
        if (_hostProcess is not null && !_hostProcess.HasExited)
        {
            _hostProcess.Kill(entireProcessTree: true);
            _hostProcess.Dispose();
            _hostProcess = null;
        }
        _isHostStarted = false;
    }

    private static bool IsTestHostResponding()
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            return httpClient.GetAsync(BaseUrl).Result.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string GetTestHostPath()
    {
        // The working directory at test runtime is usually
        // <repo>/.../Bit.BlazorUI.Tests.Performance/bin/Debug/<tfm>, but it can also be
        // the project root when run from some IDEs. Walk upward looking for the test
        // project root and resolve from there.
        const string testHostCsproj = "Bit.BlazorUI.Tests.Performance.TestHost.csproj";
        const string testHostFolder = "Bit.BlazorUI.Tests.Performance.TestHost";
        const string siblingTestProject = "Bit.BlazorUI.Tests.Performance.csproj";

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int depth = 0; depth < 8 && dir is not null; depth++)
        {
            // Same level as our test project root → the host is a sibling folder.
            if (dir.GetFiles(siblingTestProject).Length > 0 && dir.Parent is not null)
            {
                var sibling = Path.Combine(dir.Parent.FullName, testHostFolder, testHostCsproj);
                if (File.Exists(sibling))
                {
                    return sibling;
                }
            }

            // Be lenient: maybe the host folder lives directly under the current dir.
            var direct = Path.Combine(dir.FullName, testHostFolder, testHostCsproj);
            if (File.Exists(direct))
            {
                return direct;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate {testHostCsproj}. Working directory: {Directory.GetCurrentDirectory()}");
    }

    /// <summary>
    /// Waits for the component rendering to complete.
    /// </summary>
    protected async Task WaitForRenderComplete()
    {
        await Page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Rendered'",
            new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
    }

    /// <summary>
    /// Waits for the re-render to complete.
    /// </summary>
    protected async Task WaitForReRenderComplete()
    {
        await Page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Re-rendered'",
            new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
    }

    /// <summary>
    /// Gets the render time from the page.
    /// </summary>
    protected async Task<double> GetRenderTime()
    {
        var text = await Page.Locator("#render-time").TextContentAsync();
        return double.TryParse(text, out var value) ? value : 0;
    }

    /// <summary>
    /// Gets the re-render time from the page.
    /// </summary>
    protected async Task<double> GetReRenderTime()
    {
        var text = await Page.Locator("#rerender-time").TextContentAsync();
        return double.TryParse(text, out var value) ? value : 0;
    }

    /// <summary>
    /// Gets the component count from the page.
    /// </summary>
    protected async Task<int> GetComponentCount()
    {
        var text = await Page.Locator("#component-count").TextContentAsync();
        return int.TryParse(text, out var value) ? value : 0;
    }

    /// <summary>
    /// Gets the current memory usage in MB using browser Performance API.
    /// </summary>
    protected async Task<double> GetMemoryUsageMB()
    {
        try
        {
            var result = await Page.EvaluateAsync<double?>(@"() => {
                if (performance.memory) {
                    return performance.memory.usedJSHeapSize / (1024 * 1024);
                }
                return null;
            }");
            return result ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Opens a performance page (<paramref name="path"/>, one of Bit.BlazorUI.Tests.Performance.TestHost.Client's) and waits
    /// for it to be interactive - its SignalR circuit up, or its WebAssembly runtime started - failing unless it runs in the
    /// render mode the run asked for (<see cref="RenderMode"/>), so a run never measures one mode under the other's name.
    /// </summary>
    protected async Task OpenPerformancePageAsync(string path)
    {
        await Page.GotoAsync($"{BaseUrl}{path}");
        await WaitForStatus("Ready");

        var expected = RenderMode == "wasm" ? "WebAssembly" : "Server";
        var renderer = await Page.Locator("#renderer").TextContentAsync();
        Assert.AreEqual(expected, renderer, $"{path} runs on the {renderer} renderer, where PERF_RENDER_MODE={RenderMode} asks for {expected}.");
    }

    /// <summary>
    /// Waits for the page status element to show the given text.
    /// </summary>
    protected async Task WaitForStatus(string status)
    {
        await Page.WaitForFunctionAsync(
            "(expected) => document.getElementById('status')?.innerText === expected",
            status,
            new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
    }

    /// <summary>
    /// Forces a full server-side GC collection and returns the total managed heap
    /// size in MB, as reported by the test host's /api/gc-info endpoint.
    /// Use this to verify that server memory returns to near-baseline after
    /// components are disposed across mount/unmount cycles.
    /// </summary>
    /// <param name="settle">
    /// When true, waits a short period and forces a second GC before sampling. This
    /// gives the SignalR circuit and finalizer thread a chance to release any
    /// objects whose disposal is pending after a DOM-level unmount, producing a
    /// much more stable reading for memory-leak assertions.
    /// </param>
    protected async Task<double> GetServerGCMemoryMB(bool settle = true)
    {
        if (settle)
        {
            // First pass: queues finalizers, second pass: collects them.
            await _sharedHttpClient.GetAsync($"{BaseUrl}/api/gc-info");
            await Task.Delay(250);
        }

        HttpResponseMessage response;
        try
        {
            response = await _sharedHttpClient.GetAsync($"{BaseUrl}/api/gc-info");
        }
        catch (TaskCanceledException)
        {
            Assert.Fail("GetServerGCMemoryMB: /api/gc-info request timed out. The test host may be unresponsive.");
            return 0; // unreachable, but satisfies the compiler
        }
        catch (Exception ex)
        {
            Assert.Fail($"GetServerGCMemoryMB: /api/gc-info request failed: {ex.Message}");
            return 0; // unreachable, but satisfies the compiler
        }

        if (!response.IsSuccessStatusCode)
        {
            Assert.Fail($"GetServerGCMemoryMB: /api/gc-info returned {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("allocatedMB").GetDouble();
        }
        catch (Exception ex)
        {
            Assert.Fail($"GetServerGCMemoryMB: failed to parse 'allocatedMB' from /api/gc-info response. {ex.Message}. Body: {json[..Math.Min(json.Length, 200)]}");
            return 0; // unreachable
        }
    }

    /// <summary>
    /// Records one value the test measured for the run's results file (<see cref="PerformanceResults"/>), under the test's
    /// name: <paramref name="count"/> is the size of what was measured - the components rendered, the cycles run.
    /// </summary>
    protected void Record(string metric, double value, string unit, int? count = null)
    {
        // The BROWSER asked for, with the engine and version that ran.
        PerformanceResults.Add(new(TestContext.TestName ?? GetType().Name, metric, Math.Round(value, 3), unit, count),
                               _browser is null ? BrowserName : $"{BrowserName} ({_browser.BrowserType.Name} {_browser.Version})");
    }

    /// <summary>
    /// Records what an interaction cost on both sides: the page's own stopwatch, from the handler to its OnAfterRender - on
    /// the server, or in wasm mode in the browser's .NET runtime, recorded as <c>.server</c> in either - and the browser's
    /// (<see cref="MeasureInBrowserAsync"/>).
    /// </summary>
    protected void RecordInteraction(string metric, int count, double serverMs, BrowserTiming browser)
    {
        Record($"{metric}.server", serverMs, "ms", count);
        Record($"{metric}.browser", browser.Duration, "ms", count);
        Record($"{metric}.domNodes", browser.DomNodes, "nodes", count);
        if (browser.LongTasks is { } longTasks) Record($"{metric}.longTasks", longTasks, "tasks", count);
        if (browser.LongTaskTime is { } longTaskTime) Record($"{metric}.longTaskTime", longTaskTime, "ms", count);
    }

    /// <summary>
    /// What an interaction took in the browser, the way it is seen there (<see cref="MeasureInBrowserAsync"/>).
    /// </summary>
    /// <param name="Duration">From the click reaching the page to the frame after the status showed its end.</param>
    /// <param name="LongTasks">The tasks of 50 ms or more the main thread ran in between, wholly or in part; null where the browser does not report them (all but Chromium).</param>
    /// <param name="LongTaskTime">How much of those tasks' time fell in between: a task begun before the click (the click's own) or ended after the end counts for its part inside.</param>
    /// <param name="DomNodes">The elements on the page once it was over.</param>
    public sealed record BrowserTiming(double Duration, int? LongTasks, double? LongTaskTime, int DomNodes);

    // Arms the page for one interaction: performance.mark "bit-perf:start" as the trigger's click reaches the document,
    // "bit-perf:end" in the frame after the #status text reads the given end - after an animation frame and the task
    // after it, so the layout and paint of what was rendered are inside - and performance.measure "bit-perf" between
    // them, which a trace of the page shows as well. A long task counts for the part of it inside that window, and the
    // entries the observer holds but has not delivered yet are taken before it stops. The answer is kept on the window for
    // MeasureInBrowserAsync to await.
    private const string ArmInteractionScript = """
        (done) => {
            for (const name of ['bit-perf:start', 'bit-perf:end']) performance.clearMarks(name);
            performance.clearMeasures('bit-perf');

            const longTasks = [];
            const observer = PerformanceObserver.supportedEntryTypes?.includes('longtask')
                ? new PerformanceObserver(list => longTasks.push(...list.getEntries()))
                : null;
            observer?.observe({ type: 'longtask' });

            window.__bitPerfInteraction = new Promise(resolve => {
                document.addEventListener('click', () => performance.mark('bit-perf:start'), { capture: true, once: true });

                const status = document.getElementById('status');
                const watcher = new MutationObserver(() => {
                    if (status.textContent !== done || performance.getEntriesByName('bit-perf:start').length === 0) return;
                    watcher.disconnect();

                    requestAnimationFrame(() => setTimeout(() => {
                        performance.mark('bit-perf:end');
                        const measure = performance.measure('bit-perf', 'bit-perf:start', 'bit-perf:end');
                        if (observer) {
                            longTasks.push(...observer.takeRecords());
                            observer.disconnect();
                        }

                        const end = measure.startTime + measure.duration;
                        const inside = longTasks.map(t => Math.min(t.startTime + t.duration, end) - Math.max(t.startTime, measure.startTime))
                                                .filter(overlap => overlap > 0);
                        resolve({
                            duration: measure.duration,
                            longTasks: observer ? inside.length : null,
                            longTaskTime: observer ? inside.reduce((total, overlap) => total + overlap, 0) : null,
                            domNodes: document.getElementsByTagName('*').length,
                        });
                    }, 0));
                });
                watcher.observe(status, { childList: true, characterData: true, subtree: true });
            });
        }
        """;

    /// <summary>
    /// Clicks <paramref name="trigger"/> and times, in the browser, what it set off until the #status element reads
    /// <paramref name="done"/>: from the click reaching the page to the next frame after that text appeared - the round
    /// trip, the render, the DOM it changed and the layout and paint of it - as performance.mark / performance.measure
    /// ("bit-perf"). The page's own #render-time is the .NET half of it alone: the server's, or in wasm mode the browser
    /// runtime's. Fails once <see cref="DefaultTimeout"/>
    /// has passed without the status reading <paramref name="done"/>.
    /// </summary>
    protected async Task<BrowserTiming> MeasureInBrowserAsync(ILocator trigger, string done)
    {
        await Page.EvaluateAsync(ArmInteractionScript, done);
        await trigger.ClickAsync();

        // An evaluation has no timeout of its own: it would await the page's promise for as long as the status never
        // reads its end. The one left pending here is rejected when the test's context is closed.
        JsonElement timing;
        try
        {
            timing = await Page.EvaluateAsync<JsonElement>("() => window.__bitPerfInteraction").WaitAsync(TimeSpan.FromMilliseconds(DefaultTimeout));
        }
        catch (System.TimeoutException exception)
        {
            throw new System.TimeoutException($"#status did not read '{done}' within {DefaultTimeout} ms of the click.", exception);
        }

        return new(timing.GetProperty("duration").GetDouble(),
                   timing.GetProperty("longTasks").ValueKind is JsonValueKind.Number ? timing.GetProperty("longTasks").GetInt32() : null,
                   timing.GetProperty("longTaskTime").ValueKind is JsonValueKind.Number ? timing.GetProperty("longTaskTime").GetDouble() : null,
                   timing.GetProperty("domNodes").GetInt32());
    }

    /// <summary>
    /// Asserts that a metric is within the threshold.
    /// </summary>
    protected static void AssertWithinThreshold(double actual, double threshold, string metricName)
    {
        if (actual > threshold)
        {
            Assert.Fail($"{metricName}: {actual:F2} exceeded threshold of {threshold:F2}");
        }
    }

    // The last lines the host wrote, for the message of a host that exited before it could serve.
    private sealed class HostOutputTail
    {
        private const int MaxLines = 200;
        private readonly Queue<string> _lines = new();

        public void Add(string? line)
        {
            if (line is null) return;

            lock (_lines)
            {
                _lines.Enqueue(line);
                if (_lines.Count > MaxLines) _lines.Dequeue();
            }
        }

        public override string ToString()
        {
            lock (_lines)
            {
                return string.Join(Environment.NewLine, _lines);
            }
        }
    }
}
