using System.Diagnostics;
using Microsoft.Playwright;

namespace Bit.Bdnd.Tests.E2E.Infrastructure;

/// <summary>
/// The WinForms BlazorWebView harness (<c>Tests/Bit.Bdnd.Tests.Harness.Hybrid</c>), driven over the Chrome DevTools
/// Protocol: the host opens WebView2's debugging port, and Playwright attaches to it like to any Chromium. The page
/// it drives is the one inside the app window, with the Blazor Hybrid renderer running out of the browser's process.
/// </summary>
public sealed class HybridHarnessHost : IAsyncDisposable
{
    /// <summary>The origin BlazorWebView serves the app from.</summary>
    public const string AppOrigin = "https://0.0.0.1";

    private readonly ChildProcess _process;
    private readonly string _userDataFolder;
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;

    private HybridHarnessHost(ChildProcess process, string userDataFolder, IPlaywright playwright, IBrowser browser, IPage page)
    {
        _process = process;
        _userDataFolder = userDataFolder;
        _playwright = playwright;
        _browser = browser;
        Page = page;
    }

    public IPage Page { get; }

    // Bdnd/Tests/Bit.Bdnd.Tests.Harness.Hybrid, beside the demo's Bdnd/Demo/Server
    public static string Project() => Path.Combine(
        Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(RepoLayout.DemoServerProject())))!,
        "Tests", "Bit.Bdnd.Tests.Harness.Hybrid", "Bit.Bdnd.Tests.Harness.Hybrid.csproj");

    public static string Executable() => Path.Combine(
        Path.GetDirectoryName(Project())!, "bin", E2EEnvironment.Configuration, "net11.0-windows", "Bit.Bdnd.Tests.Harness.Hybrid.exe");

    public static async Task<HybridHarnessHost> StartAsync()
    {
        if (OperatingSystem.IsWindows() is false)
            throw new PlatformNotSupportedException("The BlazorWebView harness runs on WebView2, which needs Windows.");

        if (_buildFailure is { } failure) throw new InvalidOperationException(failure);
        var executable = Executable();
        if (File.Exists(executable) is false)
            throw new FileNotFoundException($"The hybrid harness has not been built for {E2EEnvironment.Configuration}: build {Project()}.", executable);

        var port = ChildProcess.FreePort();
        var userDataFolder = Path.Combine(Path.GetTempPath(), "bit-bdnd-hybrid-" + Guid.NewGuid().ToString("N"));
        var process = ChildProcess.Start(executable,
            ["--remote-debugging-port", port.ToString(), "--user-data-folder", userDataFolder],
            Path.GetDirectoryName(executable)!);

        IPlaywright? playwright = null;
        try
        {
            var debuggerUrl = $"http://127.0.0.1:{port}";
            await process.WaitForHttpAsync(debuggerUrl + "/json/version", TimeSpan.FromMinutes(2));

            playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.ConnectOverCDPAsync(debuggerUrl);
            var page = await FindAppPageAsync(browser, TimeSpan.FromMinutes(1));

            return new HybridHarnessHost(process, userDataFolder, playwright, browser, page);
        }
        catch
        {
            playwright?.Dispose();
            await process.DisposeAsync();
            await DeleteUserDataFolderAsync(userDataFolder);
            throw;
        }
    }

    private static string? _buildFailure;

    /// <summary>
    /// Builds the harness before the run (on Windows, with the Chromium pass, unless <c>BDND_E2E_SKIP_BUILD=1</c>):
    /// incrementally, so an up-to-date harness costs a few seconds, and before the demo starts, so a build that does
    /// rewrite the library's module cannot do it under a running demo. Never throws; a failure is what the hybrid
    /// tests then report.
    /// </summary>
    public static async Task PrepareAsync()
    {
        if (OperatingSystem.IsWindows() is false || E2EEnvironment.BrowserEngine != "chromium" || E2EEnvironment.SkipBuild) return;
        try
        {
            using var build = Process.Start(new ProcessStartInfo("dotnet", ["build", Project(), "-c", E2EEnvironment.Configuration, "-nologo", "-v:q"])
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            })!;
            var output = build.StandardOutput.ReadToEndAsync();
            var errors = build.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            try
            {
                await build.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                build.Kill(entireProcessTree: true);
                _buildFailure = "Building the hybrid harness took longer than 15 minutes.";
                return;
            }
            if (build.ExitCode != 0) _buildFailure = $"Building the hybrid harness failed:{Environment.NewLine}{await output}{await errors}";
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            _buildFailure = "The hybrid harness could not be built: " + ex.Message;
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

    // WebView2's browser processes can hold files in the folder for a moment after the host goes. Never throws.
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

        _playwright.Dispose();
        await _process.DisposeAsync();
        await DeleteUserDataFolderAsync(_userDataFolder);
    }
}

/// <summary>
/// The hybrid app every <c>HybridHostingTests</c> test drives: one window, started on first use. A failed start is
/// remembered, so the rest of the class fails fast instead of waiting out the startup timeout once per test.
/// </summary>
public static class HybridSession
{
    private static Lazy<Task<HybridHarnessHost>> _host = new(HybridHarnessHost.StartAsync);

    public static Task<HybridHarnessHost> GetAsync() => _host.Value;

    public static async Task StopAsync()
    {
        if (_host.IsValueCreated is false) return;

        try
        {
            await (await _host.Value).DisposeAsync();
        }
        catch (Exception)
        {
            // A host that failed to start already failed the tests that needed it.
        }

        _host = new(HybridHarnessHost.StartAsync);
    }
}
