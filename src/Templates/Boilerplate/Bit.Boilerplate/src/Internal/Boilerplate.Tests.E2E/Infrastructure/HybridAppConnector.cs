namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Launches an installed Blazor Hybrid app and attaches Playwright to its WebView over the Chrome DevTools Protocol,
/// so the Windows (WebView2) and Android (Android WebView) apps are driven exactly like a web page.
/// <para>
/// A machine running these tests is assumed to be prepared like this:
/// the Windows apps are installed through their Velopack setup (see the setup urls in <see cref="DeployedApps"/>),
/// which puts them at <c>%LocalAppData%\{appId}\current\{appId}.exe</c>, and exactly one Android device or emulator
/// is connected through adb with both Android apps installed - or, when adb sees nothing, an AVD with them installed
/// exists locally, and the first one is booted automatically.
/// </para>
/// <para>
/// Unlike a launched browser, the attached browser already contains the app's context and page: use the session's
/// <c>Page</c> instead of creating a new one.
/// </para>
/// </summary>
public static class HybridAppConnector
{
    /// <summary>
    /// How long a freshly started app gets to bring up its WebView's CDP endpoint and first page. Generous because a
    /// cold start includes the Velopack update check on Windows and process + WebView spin-up on Android.
    /// </summary>
    private static readonly TimeSpan connectDeadline = TimeSpan.FromMinutes(1);

    extension(IPlaywright playwright)
    {
        /// <summary>
        /// Starts the installed Client.Windows app identified by <paramref name="windowsAppId"/>
        /// (e.g. <see cref="DeployedApps.TodoWindowsAppId"/>) and attaches to it.
        /// <para>
        /// Every Client.Windows app hard-codes <c>--remote-debugging-port=9222</c> (see Client.Windows/Program.cs), so
        /// only one Windows app can be driven at a time, and a leftover instance of any of them would be the one
        /// answering on the port - this session would silently drive the wrong window. Hence every Client.Windows
        /// process still running is stopped first: on a test machine a fresh app per session is worth more than any
        /// window someone left open.
        /// </para>
        /// </summary>
        public async Task<HybridAppSession> LaunchWindowsApp(string windowsAppId, int port = 9222)
        {
            var exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), windowsAppId, "current", $"{windowsAppId}.exe");

            if (File.Exists(exePath) is false)
                throw new InvalidOperationException($"'{exePath}' does not exist. Install the app from its setup exe first (see {nameof(DeployedApps)}).");

            StopWindowsApps();

            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });

            var browser = await ConnectWithRetry(playwright, $"http://localhost:{port}");

            return new HybridAppSession(browser, stopApp: () =>
            {
                StopWindowsApps();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// (Re)starts the Client.Maui Android app identified by <paramref name="applicationId"/>
        /// (e.g. <see cref="DeployedApps.TodoAndroidAppId"/>) on the connected device/emulator and attaches to it.
        /// The WebView's CDP endpoint is an abstract socket on the device, so it is forwarded to
        /// <paramref name="localPort"/> first - a port other than 9222 by default, so a Windows app session can exist
        /// alongside an Android one.
        /// </summary>
        public async Task<HybridAppSession> LaunchAndroidApp(string applicationId, int localPort = 9223)
        {
            await EnsureAndroidDeviceOnline();

            // Force-stopped first so each session drives a freshly started app rather than whatever state a previous
            // run or a human left behind on the device.
            await RunAdb($"shell am force-stop {applicationId}");
            await RunAdb($"shell monkey -p {applicationId} -c android.intent.category.LAUNCHER 1");

            var deadline = DateTimeOffset.UtcNow + connectDeadline;
            string pid;

            while (true)
            {
                pid = (await RunAdb($"shell pidof {applicationId}", allowNonZeroExit: true)).Trim();

                if (string.IsNullOrWhiteSpace(pid) is false)
                    break;

                if (DateTimeOffset.UtcNow >= deadline)
                    throw new TimeoutException($"'{applicationId}' did not start on the connected Android device/emulator within {connectDeadline}. Is it installed?");

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            await RunAdb($"forward tcp:{localPort} localabstract:webview_devtools_remote_{pid}");

            var browser = await ConnectWithRetry(playwright, $"http://localhost:{localPort}");

            return new HybridAppSession(browser, stopApp: async () =>
            {
                await RunAdb($"forward --remove tcp:{localPort}", allowNonZeroExit: true);
                await RunAdb($"shell am force-stop {applicationId}");
            });
        }
    }

    private static void StopWindowsApps()
    {
        foreach (var process in Process.GetProcesses().Where(p => p.ProcessName.EndsWith(".Client.Windows", StringComparison.Ordinal)))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
    }

    /// <summary>
    /// The CDP endpoint appears some time after the app process does (and its page later still), so a single connect
    /// attempt right after launch would routinely lose the race - this keeps trying until <see cref="connectDeadline"/>.
    /// </summary>
    private static async Task<IBrowser> ConnectWithRetry(IPlaywright playwright, string cdpUrl)
    {
        var deadline = DateTimeOffset.UtcNow + connectDeadline;

        while (true)
        {
            try
            {
                var browser = await playwright.Chromium.ConnectOverCDPAsync(cdpUrl);

                if (browser.Contexts.SelectMany(c => c.Pages).Any())
                {
                    foreach (var context in browser.Contexts)
                        context.SetDefaultTimeout((float)TimeSpan.FromMinutes(1).TotalMilliseconds);

                    return browser;
                }

                // Connected before the WebView opened its page; disconnect and try again.
                await browser.CloseAsync();
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }

            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException($"No CDP endpoint with a page appeared at {cdpUrl} within {connectDeadline}.");

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }

    /// <summary>
    /// A connected device is the assumption, but when adb sees none, the first locally available AVD is booted
    /// instead - and deliberately left running afterwards: booting costs minutes and the next session reuses it.
    /// </summary>
    private static async Task EnsureAndroidDeviceOnline()
    {
        if (await IsAnyAndroidDeviceOnline())
            return;

        var emulator = FindEmulatorExecutable();

        var avdName = (await RunProcess(emulator, "-list-avds"))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // Newer emulators mix INFO/WARNING lines into -list-avds; actual AVD names never contain spaces.
            .FirstOrDefault(line => line.Contains(' ') is false)
            ?? throw new InvalidOperationException($"No Android device/emulator is connected and '{emulator}' lists no AVD to start. Create one (with the apps installed) or connect a device.");

        var process = Process.Start(new ProcessStartInfo(emulator, $"-avd {avdName}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        // Drained but discarded: the emulator logs plenty, and an unread redirected pipe would eventually block it.
        process.OutputDataReceived += delegate { };
        process.ErrorDataReceived += delegate { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var deadline = DateTimeOffset.UtcNow + emulatorBootDeadline;

        while (true)
        {
            if (await IsAnyAndroidDeviceOnline()
                && (await RunAdb("shell getprop sys.boot_completed", allowNonZeroExit: true)).Trim() is "1")
            {
                return;
            }

            if (process.HasExited)
                throw new InvalidOperationException($"The '{avdName}' emulator exited with code {process.ExitCode} before finishing its boot.");

            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException($"The '{avdName}' emulator did not finish booting within {emulatorBootDeadline}.");

            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    private static async Task<bool> IsAnyAndroidDeviceOnline()
    {
        var output = await RunAdb("devices", allowNonZeroExit: true);

        // Skips the "List of devices attached" header; a usable row reads "<serial>\tdevice" ("offline" and
        // "unauthorized" rows are not usable).
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1)
            .Any(line => line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries) is [_, "device", ..]);
    }

    /// <summary>
    /// The emulator does not have to be on PATH the way adb is, so this looks in the usual sdk roots - the ANDROID_HOME /
    /// ANDROID_SDK_ROOT variables, the default install location, and the sdk adb itself runs from (its platform-tools' parent).
    /// </summary>
    private static string FindEmulatorExecutable()
    {
        string?[] sdkRoots =
        [
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
            FindSdkRootFromAdbOnPath(),
        ];

        var candidates = sdkRoots.OfType<string>()
            .SelectMany(root => new[] { Path.Combine(root, "emulator", "emulator.exe"), Path.Combine(root, "emulator", "emulator") });

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("No Android device/emulator is connected and no emulator executable was found next to any known sdk root. Connect a device or install the Android emulator.");
    }

    private static string? FindSdkRootFromAdbOnPath()
    {
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                if (File.Exists(Path.Combine(directory, "adb.exe")) || File.Exists(Path.Combine(directory, "adb")))
                    return Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)));
            }
            catch (Exception)
            {
                // A malformed PATH entry is not this method's problem; the next entry may still hold adb.
            }
        }

        return null;
    }

    private static readonly TimeSpan emulatorBootDeadline = TimeSpan.FromMinutes(5);

    private static async Task<string> RunAdb(string arguments, bool allowNonZeroExit = false)
    {
        return await RunProcess("adb", arguments, allowNonZeroExit,
            startFailureHint: "Is the Android SDK's platform-tools directory on PATH?");
    }

    private static async Task<string> RunProcess(string fileName, string arguments, bool allowNonZeroExit = false, string? startFailureHint = null)
    {
        var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException($"Could not start {fileName}. {startFailureHint}");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode is not 0 && allowNonZeroExit is false)
            throw new InvalidOperationException($"{fileName} {arguments} failed ({process.ExitCode}): {error}");

        return output;
    }
}

/// <summary>
/// A running hybrid app with Playwright attached to its WebView. Disposing disconnects and stops the app, so the next
/// session starts from a fresh app.
/// </summary>
public sealed class HybridAppSession(IBrowser browser, Func<Task> stopApp) : IAsyncDisposable
{
    public IBrowser Browser => browser;

    /// <summary>
    /// The page the app is showing. A hybrid app has exactly one - drive this instead of creating a new page.
    /// </summary>
    public IPage Page => browser.Contexts.SelectMany(c => c.Pages).FirstOrDefault()
        ?? throw new InvalidOperationException("The attached app exposes no page (anymore).");

    public async ValueTask DisposeAsync()
    {
        await browser.CloseAsync();
        await stopApp();
    }
}
