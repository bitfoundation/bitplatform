using System.Diagnostics;
using System.Net.Http;
using NUnit.Framework;

// NOTE: deliberately in the assembly's root test namespace (not .Infrastructure) so the
// [SetUpFixture] applies to every test in the assembly regardless of their namespace.
namespace Bit.Butil.E2ETests;

/// <summary>
/// Boots <c>Bit.Butil.Demo.Web</c> as a child process for the duration of the test session and
/// exposes the URL test fixtures should hit. Reuses an externally-running server when
/// <c>BUTIL_E2E_BASE_URL</c> is set so CI can hand-roll the boot if it wants.
/// </summary>
[SetUpFixture]
public class DemoServerFixture
{
    public static string BaseUrl { get; private set; } = string.Empty;

    private Process? _process;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var external = Environment.GetEnvironmentVariable("BUTIL_E2E_BASE_URL");
        if (!string.IsNullOrWhiteSpace(external))
        {
            BaseUrl = external.TrimEnd('/');
            await WaitForReady(BaseUrl);
            return;
        }

        // Reserve a TCP port up-front so we can pass it to the dev server explicitly. Avoids
        // collisions with the developer's ambient `dotnet run` on 5040.
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var demoCsproj = LocateDemoCsproj();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "run",
                    "--no-launch-profile",
                    "--project", demoCsproj,
                    "--", "--urls", BaseUrl
                },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        // Drain output so the child's stdout buffer never fills up and stalls the server.
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) TestContext.Progress.WriteLine(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) TestContext.Progress.WriteLine(e.Data); };

        if (!_process.Start())
            throw new InvalidOperationException("Failed to start the Bit.Butil demo process.");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitForReady(BaseUrl);
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        if (_process is null || _process.HasExited) return;
        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }
        catch { /* best-effort cleanup */ }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private static int GetFreePort()
    {
        // Bind to port 0 then immediately close; the OS hands us a free ephemeral port.
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string LocateDemoCsproj()
    {
        // Walk up from the test bin directory until we find the demo csproj. This makes the
        // suite robust to the test runner's chosen working directory (CLI vs IDE differ).
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "Demo", "Bit.Butil.Demo.Web", "Bit.Butil.Demo.Web.csproj");
            if (File.Exists(candidate)) return candidate;
            // Also handle running from tests/Bit.Butil.E2ETests/bin/Debug/...
            candidate = Path.Combine(dir, "..", "..", "..", "..", "..", "Demo", "Bit.Butil.Demo.Web", "Bit.Butil.Demo.Web.csproj");
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir);
        }
        throw new InvalidOperationException("Could not locate Bit.Butil.Demo.Web.csproj relative to the test binaries.");
    }

    private static async Task WaitForReady(string baseUrl)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(180);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var resp = await http.GetAsync(baseUrl + "/");
                if (resp.IsSuccessStatusCode) return;
            }
            catch
            {
                // Server not ready yet; retry.
            }
            await Task.Delay(500);
        }
        throw new TimeoutException($"Demo app at {baseUrl} did not become ready within the deadline.");
    }
}
