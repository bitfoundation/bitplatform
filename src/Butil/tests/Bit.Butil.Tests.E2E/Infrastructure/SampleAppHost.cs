using System.Diagnostics;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// Boots <c>Bit.Butil.Samples.Web</c> as a child process and hands back the URL to drive it at - or,
/// given an external URL, waits for that deployment instead and boots nothing.
/// </summary>
/// <remarks>
/// Shared by the E2E suite (through <c>DemoServerFixture</c>) and, as a linked source file, by the
/// benchmark suite. The two want slightly different things from it - the benchmarks run the sample
/// in Release and discard its output so the report stays readable, the tests keep Debug and echo it -
/// and those are the two parameters of <see cref="Start"/>. What they must not differ in is how the
/// project is found, how a port is reserved and what "ready" means, and keeping one copy is what
/// stops those from drifting apart.
/// </remarks>
public sealed class SampleAppHost : IAsyncDisposable
{
    private Process? _process;

    /// <summary>The URL the app answers at, without a trailing slash.</summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <param name="externalBaseUrl">
    /// An already-running deployment to use instead of booting one; null or blank boots the sample.
    /// </param>
    /// <param name="configuration">The <c>-c</c> to run the sample with; null leaves the default (Debug).</param>
    /// <param name="echoOutput">
    /// Whether the child's output is written to the console. Off, it is still drained - a full
    /// pipe buffer would stall the server - and a failure to start surfaces as the readiness wait
    /// timing out.
    /// </param>
    public static async Task<SampleAppHost> Start(string? externalBaseUrl, string? configuration = null, bool echoOutput = true)
    {
        var host = new SampleAppHost();

        if (string.IsNullOrWhiteSpace(externalBaseUrl) is false)
        {
            host.BaseUrl = externalBaseUrl.TrimEnd('/');
            await WaitForReady(host.BaseUrl);
            return host;
        }

        // Reserved up front so the URL can be passed to the app explicitly, rather than colliding
        // with whatever the developer already has on the default port.
        host.BaseUrl = $"http://127.0.0.1:{FreePort()}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-launch-profile");
        if (configuration is not null)
        {
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(configuration);
        }
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(RepoLayout.SampleWebProject());
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(host.BaseUrl);

        host._process = new Process { StartInfo = startInfo };

        // Written to the console rather than to a test context: these lines keep arriving on a
        // background thread long after the caller - and eventually the whole run - has finished.
        host._process.OutputDataReceived += (_, e) => { if (echoOutput && e.Data is not null) Console.WriteLine(e.Data); };
        host._process.ErrorDataReceived += (_, e) => { if (echoOutput && e.Data is not null) Console.WriteLine(e.Data); };

        if (host._process.Start() is false)
            throw new InvalidOperationException("Failed to start Bit.Butil.Samples.Web.");

        host._process.BeginOutputReadLine();
        host._process.BeginErrorReadLine();

        await WaitForReady(host.BaseUrl);
        return host;
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is null || _process.HasExited) return;

        try
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }
        catch { /* best-effort cleanup */ }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private static int FreePort()
    {
        // Bind to port 0 then immediately close; the OS hands back a free ephemeral port.
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForReady(string baseUrl)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddMinutes(3);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync(baseUrl + "/");
                if (response.IsSuccessStatusCode) return;
            }
            catch { /* not up yet */ }

            await Task.Delay(500);
        }

        throw new TimeoutException($"{baseUrl} did not become ready within three minutes.");
    }
}
