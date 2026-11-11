using Microsoft.Playwright;

namespace Bit.Belemetry.Tests.E2E.Infrastructure;

/// <summary>
/// The MAUI sample (Samples/Maui) as a Windows app, driven over the Chrome DevTools Protocol: its BlazorWebView renders
/// through WebView2, which opens a debugging port when asked to, and Playwright attaches to it like to any Chromium. The
/// page it drives is the one inside the app window, so the shared sample pages run on MAUI's Blazor Hybrid renderer,
/// with the .NET pipeline in the app's own process. Started once, for every test that needs it, the first time one asks.
/// </summary>
/// <remarks>
/// <para>
/// The sample runs unchanged, as people copy it. Its OTLP exporter posts to <c>http://localhost:4318</c>, set in code;
/// the suite points it at the receiver of a web harness host with the per-signal <c>OTEL_EXPORTER_OTLP_*_ENDPOINT</c>
/// variables, which fill in what the code left unset, and switches it to OTLP/JSON (which the receiver decodes) with
/// <c>OTEL_EXPORTER_OTLP_PROTOCOL</c>.
/// </para>
/// <para>
/// The debugging port is opened from outside the app too. In code it cannot be: MAUI builds a WebView2 environment from
/// <c>BlazorWebViewInitializing</c>'s options on Windows but then initializes the control without it, so those options
/// never take effect (the WinForms harness host can, see <see cref="HybridHarnessHost"/>). The suite sets
/// <c>WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS</c> instead, which WebView2 ignores in an elevated process; there (a CI
/// runner) the machine-wide <c>AdditionalBrowserArguments</c> policy opens it, and
/// <see cref="E2EEnvironment.MauiDebuggingPort"/> says which.
/// </para>
/// <para>
/// The app starts with every feature granted, from the file a Hybrid host keeps its consent decision in (see
/// <see cref="SeedConsent"/>): restored at startup, that decision wins over whatever the WebView's <c>localStorage</c>
/// holds, so neither a decision of an earlier run nor the user's own changes what the tests see.
/// </para>
/// <para>
/// The sample is built unpackaged and with the Windows App SDK inside it (<see cref="BuildProperties"/>): the default,
/// packaged (MSIX) build only runs once installed. Building it needs the MAUI workloads, and running it the WebView2
/// Runtime.
/// </para>
/// </remarks>
internal sealed class MauiSampleApp : IAsyncDisposable
{
    /// <summary>The origin BlazorWebView serves the app from.</summary>
    public const string AppOrigin = "https://0.0.0.1";

    public const string TargetFramework = "net11.0-windows10.0.19041.0";

    /// <summary>The <c>ServiceName</c> the sample sets, on the resource of everything it exports.</summary>
    public const string ServiceName = "belemetry-samples-maui";

    /// <summary>The web harness host whose receiver collects the app's exports.</summary>
    public const string CollectorHostMode = HybridSession.CollectorHostMode;

    public static readonly string[] BuildProperties = ["-p:WindowsPackageType=None", "-p:WindowsAppSDKSelfContained=true"];

    private static readonly Lazy<Task<MauiSampleApp>> _shared = new(StartAsync);

    // Where Belemetry keeps the decision of a Hybrid app that sets no Persistence.Directory, as the sample does not.
    private static readonly string _consentFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
        "bit-belemetry", ServiceName, "consent.json");

    // The user's own decision while the suite's replaces it: empty when there was none.
    private static readonly string _consentBackup = _consentFile + ".e2e-backup";

    private readonly ChildProcess _process;
    private readonly string _userDataFolder;
    private readonly IBrowser _browser;

    private MauiSampleApp(ChildProcess process, string userDataFolder, IBrowser browser, IPage page, CollectorClient collector)
    {
        _process = process;
        _userDataFolder = userDataFolder;
        _browser = browser;
        Page = page;
        Collector = collector;
    }

    /// <summary>The app's one page: the WebView inside its window.</summary>
    public IPage Page { get; }

    /// <summary>The receiver the app exports to.</summary>
    public CollectorClient Collector { get; }

    /// <summary>The last lines the app wrote, for failure messages.</summary>
    public string RecentOutput => _process.RecentOutput;

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
                Assert.Inconclusive($"The MAUI sample needs the MAUI workloads: run 'dotnet workload restore' in its folder. {ex.Message.Split('\n')[0]}");
            }
        }

        var executable = RepoLayout.MauiSampleExecutable(E2EEnvironment.Configuration)
            ?? throw new FileNotFoundException($"The MAUI sample has not been built for {E2EEnvironment.Configuration} ({TargetFramework}, unpackaged).");

        if (E2EEnvironment.MauiDebuggingPort is null && Environment.IsPrivilegedProcess)
            Assert.Inconclusive("Running elevated, WebView2 ignores WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS. Open the port with the "
                + @"HKLM\SOFTWARE\Policies\Microsoft\Edge\WebView2\AdditionalBrowserArguments policy and name it in BELEMETRY_E2E_MAUI_DEBUGGING_PORT, "
                + "as bit.ci.Belemetry.e2e.yml does, or run the suite unelevated.");

        var collector = new CollectorClient((await HarnessHosts.GetAsync(CollectorHostMode)).BaseUrl);

        var port = E2EEnvironment.MauiDebuggingPort ?? ChildProcess.FreePort();
        // A folder of its own: WebView2 instances that share one share a browser process, whose port would then never
        // open. Like the arguments, ignored when elevated, where the app gets its default folder.
        var userDataFolder = Path.Combine(Path.GetTempPath(), "bit-belemetry-maui-" + Guid.NewGuid().ToString("N"));
        var environment = new Dictionary<string, string>
        {
            ["WEBVIEW2_USER_DATA_FOLDER"] = userDataFolder,
            ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = $"{collector.Origin}/test-collector/v1/traces",
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = $"{collector.Origin}/test-collector/v1/logs",
            ["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"] = $"{collector.Origin}/test-collector/v1/metrics",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/json",
        };
        if (E2EEnvironment.MauiDebuggingPort is null)
            environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port}";

        SeedConsent();
        ChildProcess? process = null;

        try
        {
            process = ChildProcess.Start(executable, [], Path.GetDirectoryName(executable)!, environment);

            var debuggerUrl = $"http://127.0.0.1:{port}";
            await process.WaitForHttpAsync(debuggerUrl + "/json/version", TimeSpan.FromMinutes(2));

            var browser = await (await PlaywrightSession.PlaywrightAsync()).Chromium.ConnectOverCDPAsync(debuggerUrl);
            var page = await FindAppPageAsync(browser, TimeSpan.FromMinutes(1));

            return new MauiSampleApp(process, userDataFolder, browser, page, collector);
        }
        catch
        {
            if (process is not null) await process.DisposeAsync();
            await DeleteUserDataFolderAsync(userDataFolder);
            RestoreConsent();
            throw;
        }
    }

    /// <summary>
    /// Writes a decision granting every feature to the app's consent file, after setting aside the one there. A backup
    /// already there is the user's decision still, from a run that never put it back (a stopped debugger), so it is kept
    /// rather than replaced by that run's.
    /// </summary>
    private static void SeedConsent()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_consentFile)!);

        if (File.Exists(_consentBackup) is false)
        {
            if (File.Exists(_consentFile)) File.Copy(_consentFile, _consentBackup);
            else File.WriteAllBytes(_consentBackup, []);
        }

        File.WriteAllText(_consentFile, $$"""{"v":1,"features":4095,"time":{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}""");
    }

    /// <summary>Puts back the decision <see cref="SeedConsent"/> set aside, once the app is gone.</summary>
    private static void RestoreConsent()
    {
        try
        {
            if (File.Exists(_consentBackup) is false) return;

            if (new FileInfo(_consentBackup).Length == 0)
            {
                File.Delete(_consentFile);
                File.Delete(_consentBackup);
            }
            else
            {
                File.Move(_consentBackup, _consentFile, overwrite: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The backup stays, and the next run puts it back.
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
        RestoreConsent();
    }
}
