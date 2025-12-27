using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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

        // Wait for page to be ready
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{testHostPath}\" --urls {BaseUrl}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _hostProcess = Process.Start(startInfo);
        
        // Wait for the host to start
        var maxWait = TimeSpan.FromSeconds(30);
        var waited = TimeSpan.Zero;
        var interval = TimeSpan.FromMilliseconds(500);

        using var httpClient = new HttpClient();
        while (waited < maxWait)
        {
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
        // Navigate from test project to test host project
        var currentDir = Directory.GetCurrentDirectory();
        
        // Try to find the TestHost project
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "..", "TestHost", "Bit.BlazorUI.Tests.Performance.TestHost.csproj"),
            Path.Combine(currentDir, "..", "..", "..", "..", "TestHost", "Bit.BlazorUI.Tests.Performance.TestHost.csproj"),
            Path.Combine(currentDir, "..", "..", "..", "..", "..", "TestHost", "Bit.BlazorUI.Tests.Performance.TestHost.csproj"),
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // Fallback: search upward for the project
        var searchDir = currentDir;
        while (!string.IsNullOrEmpty(searchDir))
        {
            var testHostPath = Path.Combine(searchDir, "Performance", "TestHost", "Bit.BlazorUI.Tests.Performance.TestHost.csproj");
            if (File.Exists(testHostPath))
            {
                return testHostPath;
            }

            var testsDir = Path.Combine(searchDir, "Bit.BlazorUI.Tests", "Performance", "TestHost", "Bit.BlazorUI.Tests.Performance.TestHost.csproj");
            if (File.Exists(testsDir))
            {
                return testsDir;
            }

            searchDir = Path.GetDirectoryName(searchDir);
        }

        throw new FileNotFoundException("Could not find Bit.BlazorUI.Tests.Performance.TestHost.csproj");
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
