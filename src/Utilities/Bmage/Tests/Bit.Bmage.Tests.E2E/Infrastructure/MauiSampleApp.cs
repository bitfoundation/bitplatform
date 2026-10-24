using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// The MAUI sample (Samples/MAUI) as a Windows app, driven over the Chrome DevTools Protocol: its BlazorWebView renders
/// through WebView2, which opens a debugging port when asked to, and Playwright attaches to it like to any Chromium. The
/// page it drives is the one inside the app window, so the pages run on the real Blazor Hybrid renderer, with .NET in
/// the app's own process. Started once, for every test that needs it, the first time one asks.
/// </summary>
/// <remarks>
/// <para>
/// The port is opened from outside the app, which stays exactly the sample people copy. In code it cannot be: MAUI
/// builds a WebView2 environment from <c>BlazorWebViewInitializing</c>'s options on Windows but then initializes the
/// control without it, so those options never take effect. The suite sets <c>WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS</c>
/// instead, which WebView2 ignores in an elevated process; there (a CI runner) the machine-wide
/// <c>AdditionalBrowserArguments</c> policy opens it, and <see cref="E2EEnvironment.MauiDebuggingPort"/> says which.
/// </para>
/// <para>
/// The sample is built unpackaged and with the Windows App SDK inside it (<see cref="BuildProperties"/>): the default,
/// packaged (MSIX) build only runs once installed. Building it needs the MAUI workloads, and running it the WebView2
/// Runtime.
/// </para>
/// </remarks>
public sealed class MauiSampleApp : IAsyncDisposable
{
    /// <summary>The origin BlazorWebView serves the app from.</summary>
    public const string AppOrigin = "https://0.0.0.1";

    public const string TargetFramework = "net10.0-windows10.0.19041.0";

    public static readonly string[] BuildProperties = ["-p:WindowsPackageType=None", "-p:WindowsAppSDKSelfContained=true"];

    private static readonly Lazy<Task<MauiSampleApp>> _shared = new(StartAsync);

    private readonly ChildProcess _process;
    private readonly string _userDataFolder;
    private readonly IBrowser _browser;
    private readonly List<string> _errors = [];

    private MauiSampleApp(ChildProcess process, string userDataFolder, IBrowser browser, IPage page)
    {
        _process = process;
        _userDataFolder = userDataFolder;
        _browser = browser;
        Page = page;

        page.PageError += (_, e) => { lock (_errors) _errors.Add(e); };
        page.Console += (_, e) => { if (e.Type == "error") lock (_errors) _errors.Add(e.Text); };
    }

    /// <summary>The app's one page: the WebView inside its window.</summary>
    public IPage Page { get; }

    /// <summary>The uncaught exceptions and console errors the page reported since the last <see cref="TakeErrors"/>.</summary>
    public List<string> TakeErrors()
    {
        lock (_errors)
        {
            var errors = _errors.ToList();
            _errors.Clear();
            return errors;
        }
    }

    /// <summary>The app, started on first use. Inconclusive off Windows, where there is no WebView2 to drive.</summary>
    public static async Task<MauiSampleApp> SharedAsync()
    {
        if (OperatingSystem.IsWindows() is false)
            Assert.Inconclusive("The MAUI sample is driven through WebView2, which needs Windows.");

        return await _shared.Value;
    }

    public static async Task StopAsync()
    {
        if (_shared.IsValueCreated is false) return;

        try
        {
            await (await _shared.Value).DisposeAsync();
        }
        catch (Exception)
        {
            // An app that failed to start already failed the tests that needed it.
        }
    }

    private static async Task<MauiSampleApp> StartAsync()
    {
        var project = RepoLayout.MauiSampleProject();
        if (E2EEnvironment.SkipBuild is false)
        {
            try
            {
                await ChildProcess.RunToCompletionAsync("dotnet",
                    ["build", project, "-f", TargetFramework, "-c", E2EEnvironment.Configuration, .. BuildProperties, "-nologo", "-v:q"],
                    Path.GetDirectoryName(project)!,
                    TimeSpan.FromMinutes(20));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("NETSDK1147", StringComparison.Ordinal))
            {
                Assert.Inconclusive($"The MAUI sample needs the maui-windows workload: run 'dotnet workload install maui-windows'. {ex.Message.Split('\n')[0]}");
            }
        }

        var executable = RepoLayout.MauiSampleExecutable(E2EEnvironment.Configuration)
            ?? throw new FileNotFoundException($"The MAUI sample has not been built for {E2EEnvironment.Configuration} ({TargetFramework}, unpackaged).");

        if (E2EEnvironment.MauiDebuggingPort is null && Environment.IsPrivilegedProcess)
            Assert.Inconclusive("Running elevated, WebView2 ignores WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS. Open the port with the "
                + @"HKLM\SOFTWARE\Policies\Microsoft\Edge\WebView2\AdditionalBrowserArguments policy and name it in BMAGE_E2E_MAUI_DEBUGGING_PORT, "
                + "as bit.ci.Bmage.e2e.yml does, or run the suite unelevated.");

        var port = E2EEnvironment.MauiDebuggingPort ?? ChildProcess.FreePort();
        // A folder of its own: WebView2 instances that share one share a browser process, whose port would then never
        // open. Like the arguments, ignored when elevated, where the app gets its default folder next to the executable.
        var userDataFolder = Path.Combine(Path.GetTempPath(), "bit-bmage-maui-" + Guid.NewGuid().ToString("N"));
        var environment = new Dictionary<string, string> { ["WEBVIEW2_USER_DATA_FOLDER"] = userDataFolder };
        if (E2EEnvironment.MauiDebuggingPort is null)
            environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port}";

        var process = ChildProcess.Start(executable, [], Path.GetDirectoryName(executable)!, environment);

        try
        {
            var debuggerUrl = $"http://127.0.0.1:{port}";
            await process.WaitForHttpAsync(debuggerUrl + "/json/version", TimeSpan.FromMinutes(2));

            var browser = await (await PlaywrightSession.PlaywrightAsync()).Chromium.ConnectOverCDPAsync(debuggerUrl);
            var page = await FindAppPageAsync(browser, TimeSpan.FromMinutes(1));

            return new MauiSampleApp(process, userDataFolder, browser, page);
        }
        catch
        {
            await process.DisposeAsync();
            await DeleteUserDataFolderAsync(userDataFolder);
            throw;
        }
    }

    private static async Task<IPage> FindAppPageAsync(IBrowser browser, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var page = browser.Contexts.SelectMany(c => c.Pages).FirstOrDefault(p => p.Url.StartsWith(AppOrigin, StringComparison.OrdinalIgnoreCase));
            if (page is not null) return page;

            await Task.Delay(250);
        }

        throw new TimeoutException($"No page at {AppOrigin} appeared in the WebView within {timeout}.");
    }

    /// <summary>
    /// The browser processes WebView2 spawned can hold files in the folder for a moment after the app goes, so a failed
    /// delete is retried briefly; it is a temporary folder, so giving up after that leaves nothing that matters.
    /// </summary>
    private static async Task DeleteUserDataFolderAsync(string userDataFolder)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (Directory.Exists(userDataFolder)) Directory.Delete(userDataFolder, recursive: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == 10) return;
                await Task.Delay(200);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Detaches from the WebView; the app itself goes with the process below.
            await _browser.CloseAsync();
        }
        catch (PlaywrightException) { /* the connection died with the app */ }

        await _process.DisposeAsync();
        await DeleteUserDataFolderAsync(_userDataFolder);
    }
}
