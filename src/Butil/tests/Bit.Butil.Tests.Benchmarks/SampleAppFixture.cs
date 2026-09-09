using System.Diagnostics;
using System.Net.Sockets;

namespace ButilTests.Benchmarks;

/// <summary>
/// Boots <c>Bit.Butil.Samples.Web</c> as a child process for the run and hands back the URL to
/// measure against. Set <c>BUTIL_BENCH_BASE_URL</c> to measure an already-running deployment
/// instead - which is what you want when the figure you care about is a published, minified build
/// rather than the developer one <c>dotnet run</c> produces.
/// </summary>
/// <remarks>
/// The same shape as the E2E suite's <c>DemoServerFixture</c>, kept separate rather than shared
/// because a benchmark and a test want different things from it: this one is deliberately allowed to
/// point at an arbitrary deployment, and it must never run more than one app at a time - a second
/// process competing for the machine is exactly the noise the timings cannot afford.
/// </remarks>
internal sealed class SampleAppFixture : IAsyncDisposable
{
    private Process? _process;

    internal string BaseUrl { get; private set; } = string.Empty;

    internal async Task Start()
    {
        var external = Environment.GetEnvironmentVariable("BUTIL_BENCH_BASE_URL");
        if (string.IsNullOrWhiteSpace(external) is false)
        {
            BaseUrl = external.TrimEnd('/');
            await WaitForReady(BaseUrl);
            return;
        }

        // Reserved up front so the URL can be passed to the app explicitly, rather than colliding
        // with whatever the developer already has on the default port.
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var project = LocateSampleCsproj();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "run", "--no-launch-profile", "-c", "Release", "--project", project, "--", "--urls", BaseUrl },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        // Drained so the child's stdout buffer cannot fill and stall the server, but discarded:
        // the app's startup chatter interleaved with the measurements would make the report
        // unreadable, and a failure to start surfaces as the readiness wait timing out.
        _process.OutputDataReceived += (_, _) => { };
        _process.ErrorDataReceived += (_, _) => { };

        if (_process.Start() is false)
            throw new InvalidOperationException("Failed to start Bit.Butil.Samples.Web.");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitForReady(BaseUrl);
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
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string LocateSampleCsproj()
    {
        // Walked up from the output directory rather than resolved relative to it, so the run does
        // not depend on which working directory the shell or the IDE happened to pick.
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && directory is not null; i++)
        {
            var candidate = Path.Combine(directory, "Samples", "Bit.Butil.Samples.Web", "Bit.Butil.Samples.Web.csproj");
            if (File.Exists(candidate)) return candidate;
            directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
        }

        throw new FileNotFoundException("Could not locate Bit.Butil.Samples.Web.csproj by walking up from the output directory.");
    }

    private static async Task WaitForReady(string baseUrl)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddMinutes(3);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode) return;
            }
            catch { /* not up yet */ }

            await Task.Delay(500);
        }

        throw new TimeoutException($"{baseUrl} did not become ready within three minutes.");
    }
}
