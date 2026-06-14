using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Base class for Playwright-based performance tests.
/// Manages the test host application lifecycle.
/// </summary>
public abstract class PerformanceTestBase : PageTest
{
    private static Process? _hostProcess;
    private static readonly object _lock = new();
    private static int _testCount;
    private static bool _isHostStarted;
    private static readonly HttpClient _sharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    protected const string BaseUrl = "http://localhost:5280";
    protected const int DefaultTimeout = 30000;

    /// <summary>
    /// Performance thresholds in milliseconds.
    /// </summary>
    protected static class Thresholds
    {
        // Initial render thresholds (ms)
        public const double Render10Components = 500;
        public const double Render100Components = 1000;
        public const double Render500Components = 3000;
        public const double Render1000Components = 5000;

        // Re-render thresholds (ms)
        public const double ReRender10Components = 200;
        public const double ReRender100Components = 500;
        public const double ReRender500Components = 1500;
        public const double ReRender1000Components = 3000;

        // Memory thresholds (MB)
        public const double Memory10Components = 50;
        public const double Memory100Components = 100;
        public const double Memory500Components = 200;
        public const double Memory1000Components = 400;
    }

    [TestInitialize]
    public async Task TestInitializeBase()
    {
        lock (_lock)
        {
            _testCount++;
            if (!_isHostStarted)
            {
                StartTestHost();
                _isHostStarted = true;
            }
        }

        // Wait for page to be ready.
        // NOTE: Do NOT use WaitForLoadStateAsync(NetworkIdle) here - Blazor Server keeps a
        // persistent SignalR WebSocket open, which Playwright counts as an active connection
        // and therefore NetworkIdle never fires. GotoAsync already waits for the Load event.
        await Page.GotoAsync(BaseUrl);
    }

    [TestCleanup]
    public void TestCleanupBase()
    {
        lock (_lock)
        {
            _testCount--;
            // Stop host when no more tests are running
            if (_testCount == 0)
            {
                StopTestHost();
            }
        }
    }

    private static void StartTestHost()
    {
        var testHostPath = GetTestHostPath();

        // The test host project targets multiple frameworks (net8.0/net9.0/net10.0).
        // 'dotnet run' refuses to pick one automatically and exits immediately, which
        // previously caused all browser tests to fail with a misleading 30-second timeout.
        // Run the host on the same TFM as the test runner so the right one is used.
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

        // 'dotnet run' performs an implicit build on first invocation, which can take
        // longer than 30s on a clean machine or in CI, so allow up to 2 minutes.
        var maxWait = TimeSpan.FromSeconds(120);
        var waited = TimeSpan.Zero;
        var interval = TimeSpan.FromMilliseconds(500);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        while (waited < maxWait)
        {
            if (_hostProcess is not null && _hostProcess.HasExited)
            {
                var stderr = _hostProcess.StandardError.ReadToEnd();
                var stdout = _hostProcess.StandardOutput.ReadToEnd();
                throw new InvalidOperationException(
                    $"Test host process exited before it could serve requests. " +
                    $"ExitCode={_hostProcess.ExitCode}.{Environment.NewLine}" +
                    $"STDERR:{Environment.NewLine}{stderr}{Environment.NewLine}" +
                    $"STDOUT:{Environment.NewLine}{stdout}");
            }

            try
            {
                var response = httpClient.GetAsync(BaseUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Host not ready yet
            }

            Thread.Sleep(interval);
            waited += interval;
        }

        throw new InvalidOperationException($"Test host failed to start within {maxWait.TotalSeconds} seconds");
    }

    private static void StopTestHost()
    {
        if (_hostProcess is not null && !_hostProcess.HasExited)
        {
            _hostProcess.Kill(entireProcessTree: true);
            _hostProcess.Dispose();
            _hostProcess = null;
        }
        _isHostStarted = false;
    }

    private static string GetTestHostPath()
    {
        // The working directory at test runtime is usually
        // <repo>/.../Bit.BlazorUI.Tests.Performance/bin/Debug/<tfm>, but it can also be
        // the project root when run from some IDEs. Walk upward looking for the test
        // project root and resolve from there.
        const string testHostCsproj = "Bit.BlazorUI.Tests.Performance.TestHost.csproj";
        const string testHostFolder = "Bit.BlazorUI.Tests.Performance.TestHost";
        const string siblingTestProject = "Bit.BlazorUI.Tests.Performance.csproj";

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int depth = 0; depth < 8 && dir is not null; depth++)
        {
            // Same level as our test project root → the host is a sibling folder.
            if (dir.GetFiles(siblingTestProject).Length > 0 && dir.Parent is not null)
            {
                var sibling = Path.Combine(dir.Parent.FullName, testHostFolder, testHostCsproj);
                if (File.Exists(sibling))
                {
                    return sibling;
                }
            }

            // Be lenient: maybe the host folder lives directly under the current dir.
            var direct = Path.Combine(dir.FullName, testHostFolder, testHostCsproj);
            if (File.Exists(direct))
            {
                return direct;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate {testHostCsproj}. Working directory: {Directory.GetCurrentDirectory()}");
    }

    /// <summary>
    /// Waits for the component rendering to complete.
    /// </summary>
    protected async Task WaitForRenderComplete()
    {
        await Page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Rendered'",
            new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
    }

    /// <summary>
    /// Waits for the re-render to complete.
    /// </summary>
    protected async Task WaitForReRenderComplete()
    {
        await Page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Re-rendered'",
            new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
    }

    /// <summary>
    /// Gets the render time from the page.
    /// </summary>
    protected async Task<double> GetRenderTime()
    {
        var text = await Page.Locator("#render-time").TextContentAsync();
        return double.TryParse(text, out var value) ? value : 0;
    }

    /// <summary>
    /// Gets the re-render time from the page.
    /// </summary>
    protected async Task<double> GetReRenderTime()
    {
        var text = await Page.Locator("#rerender-time").TextContentAsync();
        return double.TryParse(text, out var value) ? value : 0;
    }

    /// <summary>
    /// Gets the component count from the page.
    /// </summary>
    protected async Task<int> GetComponentCount()
    {
        var text = await Page.Locator("#component-count").TextContentAsync();
        return int.TryParse(text, out var value) ? value : 0;
    }

    /// <summary>
    /// Gets the current memory usage in MB using browser Performance API.
    /// </summary>
    protected async Task<double> GetMemoryUsageMB()
    {
        try
        {
            var result = await Page.EvaluateAsync<double?>(@"() => {
                if (performance.memory) {
                    return performance.memory.usedJSHeapSize / (1024 * 1024);
                }
                return null;
            }");
            return result ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Waits for the page status element to show the given text.
    /// </summary>
    protected async Task WaitForStatus(string status)
    {
        await Page.WaitForFunctionAsync(
            "(expected) => document.getElementById('status')?.innerText === expected",
            status,
            new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
    }

    /// <summary>
    /// Forces a full server-side GC collection and returns the total managed heap
    /// size in MB, as reported by the test host's /api/gc-info endpoint.
    /// Use this to verify that server memory returns to near-baseline after
    /// components are disposed across mount/unmount cycles.
    /// </summary>
    /// <param name="settle">
    /// When true, waits a short period and forces a second GC before sampling. This
    /// gives the SignalR circuit and finalizer thread a chance to release any
    /// objects whose disposal is pending after a DOM-level unmount, producing a
    /// much more stable reading for memory-leak assertions.
    /// </param>
    protected async Task<double> GetServerGCMemoryMB(bool settle = true)
    {
        if (settle)
        {
            // First pass: queues finalizers, second pass: collects them.
            await _sharedHttpClient.GetAsync($"{BaseUrl}/api/gc-info");
            await Task.Delay(250);
        }

        HttpResponseMessage response;
        try
        {
            response = await _sharedHttpClient.GetAsync($"{BaseUrl}/api/gc-info");
        }
        catch (TaskCanceledException)
        {
            Assert.Fail("GetServerGCMemoryMB: /api/gc-info request timed out. The test host may be unresponsive.");
            return 0; // unreachable, but satisfies the compiler
        }
        catch (Exception ex)
        {
            Assert.Fail($"GetServerGCMemoryMB: /api/gc-info request failed: {ex.Message}");
            return 0; // unreachable, but satisfies the compiler
        }

        if (!response.IsSuccessStatusCode)
        {
            Assert.Fail($"GetServerGCMemoryMB: /api/gc-info returned {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("allocatedMB").GetDouble();
        }
        catch (Exception ex)
        {
            Assert.Fail($"GetServerGCMemoryMB: failed to parse 'allocatedMB' from /api/gc-info response. {ex.Message}. Body: {json[..Math.Min(json.Length, 200)]}");
            return 0; // unreachable
        }
    }

    /// <summary>
    /// Asserts that a metric is within the threshold.
    /// </summary>
    protected static void AssertWithinThreshold(double actual, double threshold, string metricName)
    {
        if (actual > threshold)
        {
            Assert.Fail($"{metricName}: {actual:F2} exceeded threshold of {threshold:F2}");
        }
    }
}
