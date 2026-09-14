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

        // Passed as arguments the host applies through the WebView2 API: an elevated host (CI runners run
        // as admin) ignores --remote-debugging-port in WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS.
        var process = ChildProcess.Start(executable,
            ["--start-path", startPath, "--remote-debugging-port", port.ToString(), "--user-data-folder", userDataFolder],
            Path.GetDirectoryName(executable)!);

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
            await DeleteUserDataFolderAsync(userDataFolder);
            throw;
        }
    }

    /// <summary>
    /// Deletes a WebView2 user data folder once the app has exited. The browser processes WebView2
    /// spawned can hold files in it for a moment after the host goes, so a failed delete is retried
    /// briefly; it is a temp folder, so giving up after that leaves nothing that matters. Never throws.
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

        await DeleteUserDataFolderAsync(_userDataFolder);
    }
}
