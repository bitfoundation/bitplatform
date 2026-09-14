using Microsoft.Playwright;

namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>
/// The WinForms BlazorWebView harness, driven over the Chrome DevTools Protocol. WebView2 opens a
/// debugging port when started with <c>--remote-debugging-port</c> in
/// <c>WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS</c>, and Playwright attaches to it like to any Chromium -
/// the page it drives is the one inside the app window, running the real hybrid renderer.
/// </summary>
public sealed class HybridHarnessHost : IAsyncDisposable
{
    /// <summary>The origin BlazorWebView serves the app from.</summary>
    public const string AppOrigin = "https://0.0.0.1";

    private readonly ChildProcess _process;
    private readonly string _userDataFolder;
    private readonly IBrowser _browser;

    private HybridHarnessHost(ChildProcess process, string userDataFolder, IBrowser browser, IPage page)
    {
        _process = process;
        _userDataFolder = userDataFolder;
        _browser = browser;
        Page = page;
    }

    public IPage Page { get; }

    public static async Task<HybridHarnessHost> StartAsync(string startPath = "/")
    {
        if (OperatingSystem.IsWindows() is false)
            throw new PlatformNotSupportedException("The BlazorWebView harness runs on WebView2, which needs Windows.");

        var executable = RepoLayout.HybridHostExecutable(E2EEnvironment.Configuration);
        if (File.Exists(executable) is false)
            throw new FileNotFoundException($"The hybrid harness host has not been built for {E2EEnvironment.Configuration}.", executable);

        var port = ChildProcess.FreePort();
        // A user data folder of its own: WebView2 instances sharing one share a browser process, and
        // the second app's debugging port would never open.
        var userDataFolder = Path.Combine(Path.GetTempPath(), "bit-brouter-hybrid-" + Guid.NewGuid().ToString("N"));

        var process = ChildProcess.Start(executable, ["--start-path", startPath], Path.GetDirectoryName(executable)!, new Dictionary<string, string>
        {
            ["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port}",
            ["WEBVIEW2_USER_DATA_FOLDER"] = userDataFolder,
        });

        try
        {
            var debuggerUrl = $"http://127.0.0.1:{port}";
            await process.WaitForHttpAsync(debuggerUrl + "/json/version", TimeSpan.FromMinutes(2));

            var browser = await (await PlaywrightSession.PlaywrightAsync()).Chromium.ConnectOverCDPAsync(debuggerUrl);
            var page = await FindAppPageAsync(browser, TimeSpan.FromMinutes(1));

            return new HybridHarnessHost(process, userDataFolder, browser, page);
        }
        catch
        {
            await process.DisposeAsync();
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

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Detaches from the WebView; the app itself goes with the process below.
            await _browser.CloseAsync();
        }
        catch (PlaywrightException) { /* the connection died with the app */ }

        await _process.DisposeAsync();

        try
        {
            Directory.Delete(_userDataFolder, recursive: true);
        }
        catch (IOException) { /* WebView2 may still hold files for a moment; it is a temp folder */ }
        catch (UnauthorizedAccessException) { }
    }
}
