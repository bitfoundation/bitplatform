
namespace Microsoft.Playwright;

/// <summary>
/// Launches an installed Blazor Hybrid app and attaches Playwright to its WebView over the Chrome DevTools Protocol,
/// so the Windows (WebView2) and Android apps are driven exactly like a web page. Each launch hands back that page
/// and the callback that stops what it started - hand the callback to <see cref="AppTestBase.RegisterForCleanup"/>.
/// <para>
/// A test machine is assumed to have the Windows apps installed through their Velopack setup and exactly one Android
/// device/emulator connected with both Android apps installed - or a local AVD, whose first entry is booted here.
/// </para>
/// </summary>
public static class IPlaywrightExtensions
{
    /// <summary>
    /// Generous, because a cold start includes Velopack's update check on Windows and WebView spin-up on Android.
    /// </summary>
    private static readonly TimeSpan connectDeadline = TimeSpan.FromMinutes(1);

    extension(IPlaywright playwright)
    {
        /// <summary>
        /// Starts the installed Client.Windows app identified by <paramref name="windowsAppId"/>
        /// (e.g. <see cref="DeployedApps.TodoWindowsAppId"/>) and attaches to it. Every Client.Windows app hard-codes
        /// <c>--remote-debugging-port=9222</c>, so a leftover instance of any of them would be the one answering on
        /// the port - hence every running Client.Windows process is killed first, then its data cleared (see
        /// <see cref="WindowsAppData"/>) unless <paramref name="clearAppData"/> is false - for the caller testing what
        /// the app remembers. Started minimized unless the run is headed, so a run leaves the machine's screen alone.
        /// </summary>
        public async Task<(IPage Page, Func<Task> Stop)> LaunchWindowsApp(string windowsAppId, int port = 9222, bool clearAppData = true)
        {
            var exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), windowsAppId, "current", $"{windowsAppId}.exe");

            if (File.Exists(exePath) is false)
                throw new InvalidOperationException($"'{exePath}' does not exist. Install the app from its setup exe first (see {nameof(DeployedApps)}).");

            StopWindowsApps();

            // After the kill, so nothing still holds the files open.
            WindowsAppData.BackUpOnce();
            if (clearAppData)
                WindowsAppData.Clear(windowsAppId);

            // HEADED=1 is Playwright's own switch for watching a run, so a headed run shows the app too.
            var windowStyle = Environment.GetEnvironmentVariable("HEADED") is "1" ? ProcessWindowStyle.Normal : ProcessWindowStyle.Minimized;
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, WindowStyle = windowStyle });

            var browser = await playwright.ConnectWithRetry($"http://localhost:{port}");

            var page = browser.SinglePage();

            // The answer lives in app storage, so a launch that kept it has no banner to answer.
            if (clearAppData)
                await AnswerConsentBanner(page);

            return (page, async () =>
            {
                await browser.CloseAsync();
                StopWindowsApps();
            }
            );
        }

        /// <summary>
        /// (Re)starts the Client.Maui Android app identified by <paramref name="applicationId"/>
        /// (e.g. <see cref="DeployedApps.TodoAndroidAppId"/>) on the connected device/emulator and attaches to it.
        /// The WebView's CDP endpoint is an abstract socket on the device, so it is forwarded to
        /// <paramref name="localPort"/> first - not 9222, so an Android session can coexist with a Windows one.
        /// </summary>
        public async Task<(IPage Page, Func<Task> Stop)> LaunchAndroidApp(string applicationId, int localPort = 9223,
            string? startedByLink = null, bool clearAppData = true)
        {
            await EnsureAndroidDeviceOnline();

            // Cleared, not just force-stopped: the app keeps its access token in Android Preferences, which outlives
            // both. A session inherited from an earlier run belongs to a user that run's cleanup has since deleted, so
            // the app would boot straight into UpdateSession's ResourceNotFoundException. App link verification lives
            // in the package manager rather than in app data, so OpenAndroidAppLink still routes into the app.
            // Keeping app data is for the caller that is testing what the app does with what it remembers.
            await RunAdb(clearAppData ? $"shell pm clear {applicationId}" : $"shell am force-stop {applicationId}");

            // Started BY the link, so what the app does on a cold start is observable.
            if (startedByLink is null)
                await RunAdb($"shell monkey -p {applicationId} -c android.intent.category.LAUNCHER 1");
            else
                await playwright.OpenAndroidAppLink(startedByLink);

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

            // Android's WebView has no default browser context, so it answers the Browser.setDownloadBehavior a plain
            // connect sends with "Browser context management is not supported." NoDefaults drops that call (and the
            // focus/media emulation next to it) - the same switch Playwright's own MCP server attaches to CDP with.
            var browser = await playwright.ConnectWithRetry($"http://localhost:{localPort}", noDefaults: true);

            var page = browser.SinglePage();

            // The answer lives in app storage, so a launch that kept it has no banner to answer.
            if (clearAppData)
                await AnswerConsentBanner(page);

            return (page, async () =>
            {
                await browser.CloseAsync();
                await RunAdb($"forward --remove tcp:{localPort}", allowNonZeroExit: true);
                await RunAdb($"shell am force-stop {applicationId}");
            }
            );
        }

        /// <summary>
        /// Hands <paramref name="link"/> to Android as a VIEW intent, the way tapping it in a mail app would. No
        /// package is named on purpose: what routes it into the app rather than into a browser is the app link
        /// verification of MainActivity's IntentFilter host against its /.well-known/assetlinks.json.
        /// </summary>
        public async Task OpenAndroidAppLink(string link)
        {
            await EnsureAndroidDeviceOnline();

            // Single quoted for the device's shell, which would otherwise cut a url at its first '&'.
            var output = await RunAdb($"shell am start -a android.intent.action.VIEW -d '{link}'");

            // am start reports an unresolved intent on stdout and still exits 0.
            if (output.Contains("Error:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Android did not open '{link}': {output.Trim()}");
        }

        /// <summary>
        /// Runs <paramref name="command"/> in the device's own shell, for the half of a hybrid app that is not in its
        /// WebView - a runtime permission, or the notifications Android itself is holding. A non-zero exit is returned
        /// rather than thrown: what the caller asserts on is the output.
        /// </summary>
        public async Task<string> RunAndroidShell(string command)
        {
            await EnsureAndroidDeviceOnline();

            return await RunAdb($"shell {command}", allowNonZeroExit: true);
        }

        /// <summary>The pid of the running <paramref name="applicationId"/>; empty when it is not running.</summary>
        public async Task<string> GetAndroidAppProcessId(string applicationId)
        {
            return (await RunAdb($"shell pidof {applicationId}", allowNonZeroExit: true)).Trim();
        }

        /// <summary>
        /// The CDP endpoint appears some time after the app process (and its page later still), so connecting is
        /// retried until <see cref="connectDeadline"/>.
        /// </summary>
        private async Task<IBrowser> ConnectWithRetry(string cdpUrl, bool noDefaults = false)
        {
            var deadline = DateTimeOffset.UtcNow + connectDeadline;

            while (true)
            {
                try
                {
                    var browser = await playwright.Chromium.ConnectOverCDPAsync(cdpUrl, new() { NoDefaults = noDefaults });

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
    }

    /// <summary>
    /// Answers the consent banner of an app whose storage was just cleared - the same answer, and for the same
    /// reason, as the init script AppPageTest seeds into the web contexts: unanswered, the banner covers the bottom
    /// of every page, and its own Accept competes with the Accept the invitation journey is after. Refused rather
    /// than granted, since no test has a use for what it asks about.
    /// </summary>
    private static async Task AnswerConsentBanner(IPage page)
    {
        // The app's only bottom panel (see WebAppDownloadSizeTests), and inside it the outline button is Reject -
        // neither depends on the culture the app happens to be in.
        var reject = page.Locator(".bit-pnl-cnt.bit-pnl-bottom.bit-pnl-opn .bit-btn-otl").First;

        try
        {
            await reject.ClickAsync(new() { Timeout = (float)consentBannerDeadline.TotalMilliseconds });
        }
        catch (Exception exp) when (exp is PlaywrightException or System.TimeoutException)
        {
            // A build with nothing consent-worthy wired up never renders the banner at all.
        }
    }

    /// <summary>As generous as <see cref="AppTestBase.WaitUntilInteractive"/>: the banner opens with the booted app.</summary>
    private static readonly TimeSpan consentBannerDeadline = TimeSpan.FromMinutes(2);

    /// <summary>A hybrid app shows exactly one page, and ConnectWithRetry only returns once it is there.</summary>
    private static IPage SinglePage(this IBrowser browser)
        => browser.Contexts.SelectMany(context => context.Pages).FirstOrDefault()
           ?? throw new InvalidOperationException("The attached app exposes no page.");

    internal static void StopWindowsApps()
    {
        foreach (var process in Process.GetProcesses().Where(IsWindowsApp))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(30));
            }
            catch (Exception exp) when (exp is InvalidOperationException or AggregateException or System.ComponentModel.Win32Exception)
            {
                // Already gone, or a member of the tree that exited while it was being walked.
            }
        }

        StopOrphanedWebViews();
    }

    private static bool IsWindowsApp(Process process) => process.ProcessName.EndsWith(".Client.Windows", StringComparison.Ordinal);

    /// <summary>
    /// Killing the app's process tree is not enough: WebView2's browser process outlives a client that went away, and a
    /// run that died earlier leaves one with no parent to walk down from at all. It keeps a handle on
    /// <c>%LocalAppData%\&lt;appId&gt;.WebView2</c>, which is what made <see cref="WindowsAppData.Restore"/> fail to put
    /// the machine's own data back. Each one names the folder it serves in its command line, so these can be told apart
    /// from the WebView2 of any other app on the machine - which must not be touched.
    /// </summary>
    private static void StopOrphanedWebViews()
    {
        var webViews = Process.GetProcessesByName("msedgewebview2");

        if (webViews.Length is 0)
            return;

        var commandLinesByPid = CommandLinesOf(webViews.Select(webView => webView.Id));

        foreach (var webView in webViews)
        {
            if (commandLinesByPid.TryGetValue(webView.Id, out var commandLine) is false
                || WindowsAppData.OwnsWebView2UserDataFolder(commandLine) is false)
                continue;

            try
            {
                webView.Kill(entireProcessTree: true);
                webView.WaitForExit(TimeSpan.FromSeconds(30));
            }
            catch (Exception exp) when (exp is InvalidOperationException or AggregateException or System.ComponentModel.Win32Exception)
            {
            }
        }
    }

    /// <summary>
    /// The command line of another process is not on <see cref="Process"/>; CIM has it, and asking powershell for it
    /// keeps this out of a Windows only package reference. Best effort - an empty answer only means nothing is killed.
    /// </summary>
    private static Dictionary<int, string> CommandLinesOf(IEnumerable<int> processIds)
    {
        var filter = string.Join(" or ", processIds.Select(id => $"ProcessId={id}"));

        try
        {
            var output = RunProcess("powershell",
                $"-NoProfile -NonInteractive -Command \"Get-CimInstance Win32_Process -Filter '{filter}' | ForEach-Object {{ $_.ProcessId.ToString() + '|' + $_.CommandLine }}\"",
                allowNonZeroExit: true).GetAwaiter().GetResult();

            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split('|', 2))
                .Where(parts => parts.Length is 2 && int.TryParse(parts[0], out _))
                .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
        }
        catch (Exception exp) when (exp is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// When adb sees no device, boots the first local AVD - and leaves it running, since the next session reuses it.
    /// </summary>
    private static async Task EnsureAndroidDeviceOnline()
    {
        if (await IsAnyAndroidDeviceOnline())
            return;

        var emulator = FindEmulatorExecutable();

        var avdName = (await RunProcess(emulator, "-list-avds"))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // Newer emulators mix INFO/WARNING lines into -list-avds; real AVD names never contain spaces.
            .FirstOrDefault(line => line.Contains(' ') is false)
            ?? throw new InvalidOperationException($"No Android device/emulator is connected and '{emulator}' lists no AVD to start. Create one (with the apps installed) or connect a device.");

        var process = Process.Start(new ProcessStartInfo(emulator, $"-avd {avdName}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        // Drained but discarded: an unread redirected pipe would eventually block the emulator.
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

        // Skips the "List of devices attached" header; only a "<serial>\tdevice" row is usable.
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1)
            .Any(line => line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries) is [_, "device", ..]);
    }

    /// <summary>
    /// The emulator, unlike adb, need not be on PATH, so the usual sdk roots are searched: ANDROID_HOME,
    /// ANDROID_SDK_ROOT, the default install location, and the sdk adb itself runs from.
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
                // A malformed PATH entry is not this method's problem.
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
