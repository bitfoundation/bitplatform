using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Bit.BlazorHeadlessUI.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser context of its own against the shared demo, traced and kept under
/// TestResults when the test fails, opens a demo page and waits until it is interactive, and fails the test on any
/// script error the page reported.
/// </summary>
public abstract partial class DemoTest
{
    /// <summary>The longest a single expectation waits; also the deadline of <see cref="PollAsync{T}(Func{Task{T}}, Func{T, bool}, string)"/>.</summary>
    public const int ExpectTimeout = 15_000;

    private readonly List<string> _errors = [];
    private IBrowserContext? _context;
    private PlaywrightSession.Session? _session;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The origin the demo is served from, without a trailing slash.</summary>
    protected string BaseUrl => Session.Host!.BaseUrl;

    private PlaywrightSession.Session Session => _session ?? throw new InvalidOperationException("The session is opened in TestInitialize.");

    [TestInitialize]
    public async Task OpenContextAsync()
    {
        _session = await PlaywrightSession.GetAsync();
        var reason = _session.Unavailable ?? await PlaywrightSession.EnsureBrowserAsync(_session);
        if (reason is not null)
        {
            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + reason);
            Assert.Inconclusive("The browser tests could not run: " + reason);
        }

        // Playwright's Desktop Chrome, as the original suite ran, with the light scheme pinned: the demo follows the
        // OS's until a theme is chosen, and axe's colour-contrast rule should audit the same colours on every machine.
        _context = await _session.Browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            DeviceScaleFactor = 1,
            ColorScheme = ColorScheme.Light,
        });

        // Recorded for every test, kept only for a failed one (see CloseContextAsync).
        await _context.Tracing.StartAsync(new TracingStartOptions { Title = TestContext.TestDisplayName, Screenshots = true, Snapshots = true });
    }

    [TestCleanup]
    public async Task CloseContextAsync()
    {
        var errors = TakePageErrors();

        if (_context is not null)
        {
            try
            {
                if (TestContext.CurrentTestOutcome is not UnitTestOutcome.Passed || errors.Length > 0)
                {
                    var trace = TracePath();
                    await _context.Tracing.StopAsync(new TracingStopOptions { Path = trace });
                    TestContext.AddResultFile(trace);
                }
                else
                {
                    await _context.Tracing.StopAsync();
                }
            }
            catch (PlaywrightException)
            {
                // The browser went away first; the test already failed for it.
            }

            try { await _context.CloseAsync(); }
            catch (PlaywrightException) { /* same */ }
        }

        if (errors.Length > 0)
            Assert.Fail($"The page reported {errors.Length} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, errors)}" +
                        $"{Environment.NewLine}Recent demo server output:{Environment.NewLine}{Session.Host?.RecentOutput}");
    }

    /// <summary>
    /// Opens <paramref name="path"/> in the test's context, fails on an unsuccessful response (a page whose prerender threw
    /// is a 500), and waits until the page is interactive (see <see cref="WaitForInteractiveAsync"/>).
    /// </summary>
    protected async Task<IPage> OpenDemoAsync(string path)
    {
        var page = await _context!.NewPageAsync();

        page.PageError += (_, error) => { lock (_errors) _errors.Add("page error: " + error); };
        page.Console += (_, message) =>
        {
            // Network failures of remote demo assets (avatars ...) are not script errors.
            if (message.Type == "error" && message.Text.Contains("Failed to load resource", StringComparison.Ordinal) is false)
                lock (_errors) _errors.Add("console: " + message.Text);
        };

        var response = await page.GotoAsync(BaseUrl + path, new PageGotoOptions { Timeout = 90_000 });
        if (response is not { Ok: true })
            Assert.Fail($"{path} responded {response?.Status}.{Environment.NewLine}Recent demo server output:{Environment.NewLine}{Session.Host?.RecentOutput}");

        await WaitForInteractiveAsync(page);
        return page;
    }

    /// <summary>
    /// Waits until the page answers to the keyboard and the mouse. Every page is prerendered, so its markup is on screen
    /// long before anything handles a key; neither the load event nor an idle network says when that changes. Two
    /// signals do:
    /// <list type="bullet">
    /// <item>the h1 has <c>tabindex="-1"</c>: Routes.razor's <c>FocusOnNavigate</c> focuses it, and gives it that attribute,
    /// from <c>OnAfterRenderAsync</c>, which runs only after the router's first interactive render - on WebAssembly once
    /// the runtime has loaded and rendered the whole page over its prerendered markup, on Blazor Server once the circuit
    /// has. A prerender never runs it.</item>
    /// <item><c>[data-bit-bh-live-region]</c> exists: the library's JS initializer has installed its document-level
    /// behaviours (declarative key handling, dismissable layers, the announcer's live regions).</item>
    /// </list>
    /// </summary>
    protected static Task WaitForInteractiveAsync(IPage page) => page.WaitForFunctionAsync(
        """
        () => !!window.Blazor
           && document.querySelector('[data-bit-bh-live-region]') !== null
           && document.querySelector('h1')?.getAttribute('tabindex') === '-1'
        """, null, new PageWaitForFunctionOptions { Timeout = 90_000 });

    /// <summary>The page's script errors so far, minus favicon and 404 noise, which the test no longer reports at cleanup.</summary>
    protected string[] TakePageErrors()
    {
        lock (_errors)
        {
            string[] errors = [.. _errors.Where(e => IgnoredError().IsMatch(e) is false)];
            _errors.Clear();
            return errors;
        }
    }

    /// <summary>axe-core finds nothing in <c>main</c> (see <see cref="Axe"/>).</summary>
    protected static async Task ExpectAccessibleAsync(IPage page)
    {
        var violations = await Axe.ViolationsAsync(page);
        Assert.IsEmpty(violations, $"axe-core found {violations.Length} violation(s) on {page.Url}:{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, violations)}");
    }

    /// <summary>
    /// Probes until <paramref name="condition"/> holds - the original suite's <c>expect.poll</c>, for a state no locator
    /// assertion reads (where the focus is, a computed style). Fails with the last value seen.
    /// </summary>
    protected static async Task<T> PollAsync<T>(Func<Task<T>> probe, Func<T, bool> condition, string expectation)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(ExpectTimeout);
        while (true)
        {
            var value = await probe();
            if (condition(value)) return value;
            if (DateTime.UtcNow > deadline) Assert.Fail($"Expected {expectation} within {ExpectTimeout} ms; the last value was '{value}'.");
            await Task.Delay(100);
        }
    }

    protected static Task<T> PollAsync<T>(Func<Task<T>> probe, T expected, string expectation) =>
        PollAsync(probe, value => EqualityComparer<T>.Default.Equals(value, expected), $"{expectation} to be '{expected}'");

    /// <summary>The focused element: its id, its role (else its tag name) and its text.</summary>
    protected static async Task<FocusedElement?> FocusedAsync(IPage page)
    {
        var parts = await page.EvaluateAsync<string[]?>(
            """
            () => {
              const a = document.activeElement;
              return a ? [a.id, a.getAttribute('role') ?? a.tagName.toLowerCase(), (a.textContent ?? '').trim()] : null;
            }
            """);
        return parts is null ? null : new FocusedElement(parts[0], parts[1], parts[2]);
    }

    /// <summary>
    /// TestResults/traces next to the binaries - the runner's default results folder - rather than the run's own
    /// deployment folder, which the runner names afresh for every run and worker.
    /// </summary>
    private string TracePath()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "traces");
        Directory.CreateDirectory(directory);
        var name = UnsafeFileNameCharacters().Replace(TestContext.TestDisplayName ?? TestContext.TestName ?? "test", "_");
        return Path.Combine(directory, $"{name}.zip");
    }

    [GeneratedRegex("favicon|404")]
    private static partial Regex IgnoredError();

    [GeneratedRegex(@"[^\w.()-]+")]
    private static partial Regex UnsafeFileNameCharacters();

    public sealed record FocusedElement(string Id, string Role, string Text);
}
