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
    private static IBrowser? _browser;

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

        await _browserLock.WaitAsync();
        try
        {
            _browser ??= await LaunchBrowserAsync();
        }
        finally
        {
            _browserLock.Release();
        }

        _context = await _browser.NewContextAsync(ContextOptions);
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
                    await DiscardBrowserAsync();
                }
                finally
                {
                    _context = null;
                }
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
    /// </summary>
    protected async Task OpenAsync(string path, string status = "Ready")
    {
        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : $"{BaseUrl}/{path.TrimStart('/')}";

        // Not WaitForLoadStateAsync(NetworkIdle): Blazor Server keeps its SignalR connection open, which Playwright
        // counts as network activity, so it would never fire.
        await Page.GotoAsync(url);
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
    /// Waits for two animation frames, the way a layout a script takes after a render is waited for.
    /// </summary>
    protected Task NextFramesAsync()
    {
        return Page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
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

        lock (_hostLock)
        {
            StopTestHost();
        }
    }

    private static async Task<ServerErrors> ReadServerErrorsAsync(int after, IEnumerable<string>? paths = null)
    {
        try
        {
            var query = paths is null ? string.Empty : $"&paths={Uri.EscapeDataString(string.Join(',', paths))}";
            return await _http.GetFromJsonAsync<ServerErrors>($"{BaseUrl}/api/server-errors?after={after}{query}") ?? new(0, []);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(after, []);
        }
    }

    private static async Task DiscardBrowserAsync()
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
