using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// The base of every E2E test class: it starts the test host once per run (or attaches to one already answering at
/// <c>E2E_HOST_URL</c>, <c>http://localhost:5290</c> by default), launches the browser <c>BROWSER</c> names once per
/// run (<c>chromium</c> by default, <c>edge</c>, <c>firefox</c> or <c>webkit</c>; <c>HEADED=1</c> to watch), and gives
/// each test a fresh context and page. <see cref="OpenAsync"/> navigates to a page of the host and waits for its
/// first interactive render. Every uncaught error the page raises and every console error it logs during the test
/// fails it at cleanup, unless the test opted out through <see cref="AllowConsoleErrors"/>.
/// </summary>
public abstract class E2ETestBase
{
    private static Process? _hostProcess;
    private static readonly object _hostLock = new();
    private static bool _isHostStarted;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly SemaphoreSlim _browserLock = new(1, 1);
    private static IPlaywright? _playwright;
    // Every test runs on a browser of its own, which it hands on to the next test to start once it is done; these are
    // the ones no test is on, guarded by _browserLock. The test classes that run beside each other never share one:
    // Firefox drops and merges the pointer moves of a page while other pages of its browser are being driven, so a
    // drag delivers a fraction of its moves and lands short of where it was taken, and a swipe ends somewhere else.
    private static readonly Stack<IBrowser> _idleBrowsers = new();
    // Every browser launched and not retired, for the run to close at its end.
    private static readonly List<IBrowser> _browsers = [];

    private IBrowser? _contextBrowser;
    private IBrowserContext? _context;
    private readonly List<string> _pageErrors = [];
    private readonly List<string> _consoleErrors = [];
    private int _serverErrorsBefore;
    private readonly HashSet<string> _visitedPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The URL the host answers at.
    /// </summary>
    protected static readonly string BaseUrl = (Environment.GetEnvironmentVariable("E2E_HOST_URL") is { Length: > 0 } url
                                                    ? url
                                                    : "http://localhost:5290").TrimEnd('/');

    /// <summary>
    /// How long a wait of the base class, and the default of every Playwright expectation, lasts.
    /// </summary>
    protected const int DefaultTimeout = 30000;

    /// <summary>
    /// The page of the current test, on a browser context of its own.
    /// </summary>
    protected IPage Page { get; private set; } = null!;

    /// <summary>
    /// The browser context a test class needs, for the ones that cannot be driven in the default desktop one - a
    /// test of behaviour that only runs on a touch device, or at a phone's viewport. Null takes the defaults.
    /// </summary>
    protected virtual BrowserNewContextOptions? ContextOptions => null;

    /// <summary>
    /// Lets a test end with console errors on its page, for the one whose subject is an error (a BitErrorBoundary
    /// catching one, a circuit torn down on purpose). Uncaught page errors still fail it.
    /// </summary>
    protected bool AllowConsoleErrors { get; set; }

    /// <summary>
    /// Lets a test end with uncaught page errors as well as console errors.
    /// </summary>
    protected bool AllowPageErrors { get; set; }

    /// <summary>
    /// The browser the run drives, as <c>BROWSER</c> named it: chromium, edge, firefox or webkit.
    /// </summary>
    protected static string BrowserName => (Environment.GetEnvironmentVariable("BROWSER") ?? "chromium").ToLowerInvariant() switch
    {
        "" => "chromium",
        "msedge" => "edge",
        var name => name
    };

    /// <summary>
    /// Where the pages run, as <c>E2E_RENDER_MODE</c> names it: <c>server</c> (InteractiveServer, the default) or
    /// <c>wasm</c> (InteractiveWebAssembly). The host reads it off a cookie every browser context of the run carries.
    /// </summary>
    protected static string RenderMode => (Environment.GetEnvironmentVariable("E2E_RENDER_MODE") ?? "server").ToLowerInvariant() switch
    {
        "wasm" or "webassembly" => "wasm",
        _ => "server"
    };

    /// <summary>
    /// Whether the pages run in the browser (InteractiveWebAssembly) rather than on the server.
    /// </summary>
    protected static bool IsWasm => RenderMode == "wasm";

    protected static bool IsChromium => BrowserName is "chromium" or "edge";
    protected static bool IsFirefox => BrowserName is "firefox";
    protected static bool IsWebKit => BrowserName is "webkit";

    [TestInitialize]
    public async Task E2EInitializeAsync()
    {
        lock (_hostLock)
        {
            if (_isHostStarted is false)
            {
                StartTestHost();
                _isHostStarted = true;
            }
        }

        IBrowser browser;
        await _browserLock.WaitAsync();
        try
        {
            if (_idleBrowsers.TryPop(out var idle) is false)
            {
                idle = await LaunchBrowserAsync();
                _browsers.Add(idle);
            }

            browser = idle;
            _contextBrowser = browser;
        }
        finally
        {
            _browserLock.Release();
        }

        _context = await browser.NewContextAsync(ContextOptions);
        await _context.AddCookiesAsync([new Cookie { Name = "e2e-render-mode", Value = RenderMode, Url = BaseUrl }]);
        Page = await _context.NewPageAsync();
        Page.SetDefaultTimeout(DefaultTimeout);
        Page.PageError += (_, error) => _pageErrors.Add(error);
        // The pages this test has been on, for the host to tell its errors from those of the tests beside it.
        Page.FrameNavigated += (_, frame) =>
        {
            if (frame.ParentFrame is null && Uri.TryCreate(frame.Url, UriKind.Absolute, out var uri)) _visitedPaths.Add(uri.AbsolutePath.TrimEnd('/'));
        };
        Page.Console += (_, message) =>
        {
            if (message.Type == "error") _consoleErrors.Add(message.Text);
        };

        _serverErrorsBefore = (await ReadServerErrorsAsync(0)).Count;
    }

    [TestCleanup]
    public async Task E2ECleanupAsync()
    {
        try
        {
            var broken = false;

            if (_context is not null)
            {
                try
                {
                    await _context.CloseAsync();
                }
                catch (PlaywrightException)
                {
                    // Firefox's driver can fail to tear down a context that opened popups, and a browser left in that
                    // state fails to close every context after it; the next test launches a fresh one instead.
                    broken = true;
                }
                finally
                {
                    _context = null;
                }
            }

            if (_contextBrowser is { } browser)
            {
                _contextBrowser = null;
                await ReleaseBrowserAsync(browser, broken);
            }
        }
        finally
        {
            if (AllowPageErrors is false && _pageErrors.Count > 0)
            {
                Assert.Fail($"The page raised {_pageErrors.Count} uncaught error(s):{Environment.NewLine}{string.Join(Environment.NewLine, _pageErrors)}");
            }

            if (AllowConsoleErrors is false && _consoleErrors.Count > 0)
            {
                Assert.Fail($"The page logged {_consoleErrors.Count} console error(s):{Environment.NewLine}{string.Join(Environment.NewLine, _consoleErrors)}");
            }
        }
    }

    /// <summary>
    /// Opens a page of the host (<c>/e2e/button</c>, or a full path with its query) and waits until its first
    /// interactive render has run, which every page reports through its E2EStatus ("Ready", or the status given).
    /// A page whose test holds a resource back (an image answered through Page.RouteAsync only when the test says so)
    /// passes <paramref name="waitForLoad"/> false: the navigation then returns once the document arrives instead of
    /// waiting for the load event that resource would hold up.
    /// </summary>
    protected async Task OpenAsync(string path, string status = "Ready", bool waitForLoad = true)
    {
        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : $"{BaseUrl}/{path.TrimStart('/')}";

        // Not WaitForLoadStateAsync(NetworkIdle): Blazor Server keeps its SignalR connection open, which Playwright
        // counts as network activity, so it would never fire.
        await Page.GotoAsync(url, new() { WaitUntil = waitForLoad ? WaitUntilState.Load : WaitUntilState.Commit });
        await WaitForStatusAsync(status);
    }

    /// <summary>
    /// Waits for the page's status line to show the given text.
    /// </summary>
    protected Task WaitForStatusAsync(string status, int? timeout = null)
    {
        return Expect(Page.Locator("#status")).ToHaveTextAsync(status, new() { Timeout = timeout ?? DefaultTimeout });
    }

    /// <summary>
    /// Whether Blazor has shown its error UI, which it does once the circuit is torn down by an unhandled exception.
    /// </summary>
    protected Task<bool> IsCircuitDeadAsync()
    {
        return Page.EvaluateAsync<bool>("() => getComputedStyle(document.getElementById('blazor-error-ui')).display !== 'none'");
    }

    /// <summary>
    /// The uncaught errors the page has raised so far in this test.
    /// </summary>
    protected IReadOnlyList<string> PageErrors => _pageErrors;

    /// <summary>
    /// The console errors the page has logged so far in this test.
    /// </summary>
    protected IReadOnlyList<string> ConsoleErrors => _consoleErrors;

    /// <summary>
    /// The errors the host has logged since this test began - an exception thrown while a component was disposed, a
    /// circuit torn down - which the page itself never shows. Tests run beside each other against one host, so only
    /// the errors of a circuit that was on one of the pages this test visited are read, plus the few the host cannot
    /// tie to a circuit, which may still belong to a neighbour; what such an entry says tells which.
    /// </summary>
    protected async Task<IReadOnlyList<string>> GetServerErrorsAsync()
    {
        return (await ReadServerErrorsAsync(_serverErrorsBefore, _visitedPaths)).Entries;
    }

    /// <summary>
    /// The warnings and errors the host has logged since this test began, for the pages this test visited (as
    /// <see cref="GetServerErrorsAsync"/>) - what a handled exception the framework only warns about leaves behind, such
    /// as one a BitErrorBoundary caught and logged.
    /// </summary>
    protected async Task<IReadOnlyList<string>> GetServerWarningsAndErrorsAsync()
    {
        return (await ReadServerErrorsAsync(_serverErrorsBefore, _visitedPaths, "Warning")).Entries;
    }

    /// <summary>
    /// Fails the test on any error the host logged since it began.
    /// </summary>
    protected async Task AssertNoServerErrorsAsync()
    {
        var errors = await GetServerErrorsAsync();

        Assert.AreEqual(0, errors.Count, $"The host logged {errors.Count} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    /// <summary>
    /// Reads a computed style of the first element the selector finds.
    /// </summary>
    protected Task<string> ComputedStyleAsync(string selector, string property)
    {
        return Page.Locator(selector).First.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
    }

    /// <summary>
    /// A regular expression matching one class among the classes of an element, for ToHaveClassAsync.
    /// </summary>
    protected static Regex HasClass(string className) => new($@"(^|\s){Regex.Escape(className)}(\s|$)");

    /// <summary>
    /// A regular expression matching a text with a 12-hour time in it, for ToHaveValueAsync and ToHaveTextAsync: the
    /// space before each AM and PM may also be the narrow no-break space (U+202F) the en-US patterns of ICU's data carry,
    /// which .NET reads on Linux, where on Windows it reads a plain space.
    /// </summary>
    protected static Regex TwelveHour(string text) => new($"^{Regex.Escape(text).Replace(@"\ AM", "[  ]AM").Replace(@"\ PM", "[  ]PM")}$");

    /// <summary>
    /// Waits for two animation frames, the way a layout a script takes after a render is waited for.
    /// </summary>
    protected Task NextFramesAsync()
    {
        return Page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
    }

    /// <summary>
    /// Wheels a scroller - the element the selector finds, or the page itself for "window" - with the mouse where it
    /// already is, to where one wheel of that delta takes it in Chromium: as far as asked, or to the end it has when the
    /// wheel starts. Firefox moves a scroller by no more than a page for one wheel event however large its delta is, so
    /// the way there is wheeled a page at a time, each step let come to rest - a step sent while the last one is still
    /// animating only moves that animation's target - and it stops at that end even when reaching it loads more content.
    /// </summary>
    protected async Task WheelInPagesAsync(string selector, int deltaY)
    {
        var range = await Page.EvaluateAsync<double[]>(@"s => {
            const el = s === 'window' ? document.scrollingElement : document.querySelector(s);
            return [el.scrollTop, el.scrollHeight - el.clientHeight, el.clientHeight * 0.8];
        }", selector);
        var target = Math.Clamp(range[0] + deltaY, 0, range[1]);
        for (var now = range[0]; Math.Abs(target - now) >= 1;)
        {
            await Page.Mouse.WheelAsync(0, (int)(Math.Sign(target - now) * Math.Min(Math.Abs(target - now), Math.Max(1, range[2]))));
            var after = await WaitForWheelToSettleAsync(selector, now);
            if (after == now) break;
            now = after;
        }
    }

    /// <summary>
    /// Waits for a wheel scroll to have moved the scroller and come to rest, and returns where it came to rest: a wheel
    /// scroll is animated, and a second wheel sent while the first is still running only moves the target of the same
    /// animation.
    /// </summary>
    protected Task<double> WaitForWheelToSettleAsync(string selector, double before)
    {
        return Page.EvaluateAsync<double>(@"([selector, before]) => new Promise(resolve => {
            const el = selector === 'window' ? document.scrollingElement : document.querySelector(selector);
            let last = el.scrollTop, stable = 0, frames = 0;
            const tick = () => {
                frames++;
                const now = el.scrollTop;
                if (now === last) stable++; else { stable = 0; last = now; }
                if ((now !== before && stable >= 5) || frames > 120) return resolve(now);
                requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
        })", new object[] { selector, before });
    }

    /// <summary>
    /// Gives the page the safe areas of a phone - its status bar and notch at the top, its home indicator at the bottom -
    /// as env(safe-area-inset-*) reads them, through the Chrome DevTools Protocol, which only Chromium speaks.
    /// </summary>
    protected async Task SetSafeAreaInsetsAsync(int top, int bottom)
    {
        var cdp = await Page.Context.NewCDPSessionAsync(Page);

        await cdp.SendAsync("Emulation.setSafeAreaInsetsOverride", new Dictionary<string, object>
        {
            ["insets"] = new Dictionary<string, object> { ["top"] = top, ["topMax"] = top, ["bottom"] = bottom, ["bottomMax"] = bottom }
        });
    }

    /// <summary>
    /// Closes the browsers and stops the test host the run started, once every test of it is done.
    /// </summary>
    internal static async Task ShutdownAsync()
    {
        await _browserLock.WaitAsync();
        try
        {
            foreach (var browser in _browsers)
            {
                try
                {
                    await browser.CloseAsync();
                }
                catch (PlaywrightException)
                {
                    // already unusable; the driver reaps it with the Playwright instance
                }
            }
        }
        finally
        {
            _playwright?.Dispose();
            _browsers.Clear();
            _idleBrowsers.Clear();
            _playwright = null;
            _browserLock.Release();
        }

        lock (_hostLock)
        {
            StopTestHost();
        }
    }

    private static async Task<ServerErrors> ReadServerErrorsAsync(int after, IEnumerable<string>? paths = null, string level = "Error")
    {
        var query = paths is null ? string.Empty : $"&paths={Uri.EscapeDataString(string.Join(',', paths))}";

        // A read that fails fails the test rather than being taken for "no errors": an empty answer would let the
        // test's own errors through, and a baseline of 0 would blame it for every error the host logged before it began.
        return await ReadFromHostAsync<ServerErrors>($"/api/server-errors?after={after}&level={level}{query}") ?? new(0, []);
    }

    /// <summary>
    /// Reads what an endpoint of the test host answers (an <c>/api/...</c> path with its query), for what only the host
    /// knows - what it logged, the culture data it formats with on the machine it runs on, which need not be the one the
    /// tests run on (<c>E2E_HOST_URL</c>). A read that fails is tried again, and then fails the test.
    /// </summary>
    protected static async Task<T?> ReadFromHostAsync<T>(string pathAndQuery)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await _http.GetFromJsonAsync<T>($"{BaseUrl}/{pathAndQuery.TrimStart('/')}");
            }
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }
    }

    /// <summary>
    /// Lets go of the browser a test ran on, for the next test to start to run on. A broken one is retired and closed
    /// instead - no other test is on it - and the next test to find no browser left launches a fresh one.
    /// </summary>
    private static async Task ReleaseBrowserAsync(IBrowser browser, bool broken)
    {
        await _browserLock.WaitAsync();
        try
        {
            if (broken is false)
            {
                _idleBrowsers.Push(browser);
                return;
            }

            _browsers.Remove(browser);
        }
        finally
        {
            _browserLock.Release();
        }

        try
        {
            await browser.CloseAsync();
        }
        catch (PlaywrightException)
        {
            // already unusable; the driver reaps it with the Playwright instance
        }
    }

    private static async Task<IBrowser> LaunchBrowserAsync()
    {
        _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

        SetDefaultExpectTimeout(DefaultTimeout);

        var options = new BrowserTypeLaunchOptions { Headless = Environment.GetEnvironmentVariable("HEADED") != "1" };

        return BrowserName switch
        {
            "chromium" => await _playwright.Chromium.LaunchAsync(options),
            "edge" => await _playwright.Chromium.LaunchAsync(new(options) { Channel = "msedge" }),
            "firefox" => await _playwright.Firefox.LaunchAsync(options),
            "webkit" => await _playwright.Webkit.LaunchAsync(options),
            var name => throw new InvalidOperationException($"Unknown BROWSER '{name}'. Use chromium, edge, firefox or webkit.")
        };
    }

    private static void StartTestHost()
    {
        // A host already serving the URL is used as it is: one started by hand, left running between runs so the
        // build is paid once, or shared by several runs.
        if (IsTestHostResponding()) return;

        var testHostPath = GetTestHostPath();

        // The host targets several frameworks, which `dotnet run` refuses to pick between; it runs on the runner's.
        var tfm = $"net{Environment.Version.Major}.{Environment.Version.Minor}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{testHostPath}\" --framework {tfm} --urls {BaseUrl}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _hostProcess = Process.Start(startInfo);

        // The host's output is read as it is written: a redirected stream nobody reads fills its pipe, and then the
        // host blocks on logging. Only the tail is kept, for the message below.
        var hostOutput = new HostOutputTail();
        _hostProcess!.OutputDataReceived += (_, e) => hostOutput.Add(e.Data);
        _hostProcess.ErrorDataReceived += (_, e) => hostOutput.Add(e.Data);
        _hostProcess.BeginOutputReadLine();
        _hostProcess.BeginErrorReadLine();

        // `dotnet run` builds first, which can take minutes on a clean machine.
        var maxWait = TimeSpan.FromSeconds(300);
        var waited = TimeSpan.Zero;
        var interval = TimeSpan.FromMilliseconds(500);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        while (waited < maxWait)
        {
            if (_hostProcess.HasExited)
            {
                _hostProcess.WaitForExit();
                throw new InvalidOperationException(
                    $"Test host process exited before it could serve requests. ExitCode={_hostProcess.ExitCode}.{Environment.NewLine}OUTPUT:{Environment.NewLine}{hostOutput}");
            }

            try
            {
                if (httpClient.GetAsync(BaseUrl).Result.IsSuccessStatusCode) return;
            }
            catch
            {
                // not ready yet
            }

            Thread.Sleep(interval);
            waited += interval;
        }

        throw new InvalidOperationException($"Test host failed to start within {maxWait.TotalSeconds} seconds.{Environment.NewLine}OUTPUT:{Environment.NewLine}{hostOutput}");
    }

    private static void StopTestHost()
    {
        if (_hostProcess is not null && _hostProcess.HasExited is false)
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
        const string testHostCsproj = "Bit.BlazorUI.Tests.E2E.TestHost.csproj";
        const string testHostFolder = "Bit.BlazorUI.Tests.E2E.TestHost";
        const string siblingTestProject = "Bit.BlazorUI.Tests.E2E.csproj";

        // The working directory is the test output folder (bin/Debug/<tfm>) or, from an IDE, the project root; the
        // host is the sibling folder of the test project.
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var depth = 0; depth < 8 && dir is not null; depth++)
        {
            if (dir.GetFiles(siblingTestProject).Length > 0 && dir.Parent is not null)
            {
                var sibling = Path.Combine(dir.Parent.FullName, testHostFolder, testHostCsproj);
                if (File.Exists(sibling)) return sibling;
            }

            var direct = Path.Combine(dir.FullName, testHostFolder, testHostCsproj);
            if (File.Exists(direct)) return direct;

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate {testHostCsproj}. Working directory: {Directory.GetCurrentDirectory()}");
    }

    private sealed record ServerErrors(int Count, string[] Entries);

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
