using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using NUnit.Framework;

// NOTE: deliberately in the assembly's root test namespace (not .Infrastructure) so the
// [SetUpFixture] applies to every test in the assembly regardless of their namespace.
namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// Boots <c>Bit.Butil.Demo.Server</c> - the app that hosts the Butil MCP server - as a child
/// process for the duration of the test session, and hands every fixture the origin to talk to.
/// <para>
/// A child process rather than a <c>WebApplicationFactory</c>: the MCP server is only as good as
/// the deployment around it. The catalogs are built from resources embedded by the .csproj, the
/// XML documentation is read from beside the assembly, the docs pages are rendered by the real
/// components, and the endpoint is reached through the real routing, CORS and redirect pipeline.
/// Every one of those is a way the tools can answer nothing in production while answering
/// perfectly against an in-memory host, so the suite runs the app the way the app runs.
/// </para>
/// <para>
/// Set <c>BUTIL_MCP_BASE_URL</c> to point the suite at an already-running server - a local
/// <c>dotnet run</c>, or a deployed instance - instead of launching one. That is the same switch
/// that makes this suite a smoke test against a real deployment.
/// </para>
/// </summary>
[SetUpFixture]
public class McpServerFixture
{
    /// <summary>The origin the server answers on, without a trailing slash.</summary>
    public static string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// A client for the plain HTTP surface. Redirects are not followed: the suite asserts on the
    /// status code the server actually returned, and a silent 307 to https would otherwise read as
    /// a pass.
    /// </summary>
    public static HttpClient Http { get; private set; } = null!;

    /// <summary>
    /// The very first search this server ever answered, taken before any fixture connects - or null
    /// when the suite was pointed at a server it did not start, which is nobody's cold start to
    /// catch.
    /// <para>
    /// The search index is built in the background from startup and nothing waits for it, so "the
    /// first caller still gets a real answer" is a claim about a genuinely cold app. By the time any
    /// test runs, another fixture may already have warmed it; readiness deliberately polls
    /// robots.txt, which renders nothing and searches nothing, so this is the call that arrives
    /// first.
    /// </para>
    /// </summary>
    public static string? ColdSearch { get; private set; }

    private Process? _process;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var external = Environment.GetEnvironmentVariable("BUTIL_MCP_BASE_URL");

        if (string.IsNullOrWhiteSpace(external) is false)
        {
            BaseUrl = external.TrimEnd('/');
            Http = CreateHttpClient();
            await WaitForReady(BaseUrl);
            return;
        }

        // Reserve a port up front so it can be passed to the app explicitly, rather than parsing it
        // back out of the child's stdout - and so an ambient `dotnet run` on 5040 cannot collide.
        BaseUrl = $"http://127.0.0.1:{GetFreePort()}";
        Http = CreateHttpClient();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "run",
                    "--no-launch-profile",
                    "--project", LocateDemoServerCsproj(),
                    // Its own bin and obj, away from the project's. A developer with the demo
                    // already running holds a lock on Server/bin, and without this the suite's
                    // build fails on exactly the machines where someone is working on it.
                    "--artifacts-path", ArtifactsPath(),
                    "--", "--urls", BaseUrl
                },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        // Drain both pipes so the child's buffers never fill and stall the server it is hosting.
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) TestContext.Progress.WriteLine(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) TestContext.Progress.WriteLine(e.Data); };

        if (_process.Start() is false)
            throw new InvalidOperationException("Failed to start the Bit.Butil demo server process.");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitForReady(BaseUrl);

        // Before any fixture connects, so it is genuinely the first search this process answers.
        // Asserted on in ResilienceTests rather than here: a failed assertion in a [SetUpFixture]
        // reports as every test erroring, which says nothing about what actually broke.
        using var firstSearch = await Http.GetAsync(Url("api/mcp/SearchButil?query=clipboard"));
        ColdSearch = firstSearch.IsSuccessStatusCode ? await firstSearch.Content.ReadAsStringAsync() : string.Empty;
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        Http?.Dispose();

        if (_process is null || _process.HasExited) return;

        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }
        catch
        {
            // Best-effort cleanup: the run is over either way.
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    /// <summary>An absolute URL on the server under test.</summary>
    public static Uri Url(string relativePath) => new($"{BaseUrl}/{relativePath.TrimStart('/')}");

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };

        // Generous: the first call to a document tool renders a page, and the first search builds
        // the whole index if the background warm-up has not finished yet.
        return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(2) };
    }

    private static int GetFreePort()
    {
        // Bind to port 0 and close immediately; the OS hands back a free ephemeral port.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    /// <summary>
    /// Where the child's build output goes. Stable across runs so the build stays incremental, and
    /// overridable so CI can put it somewhere it caches.
    /// </summary>
    private static string ArtifactsPath()
    {
        var configured = Environment.GetEnvironmentVariable("BUTIL_MCP_ARTIFACTS_PATH");

        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "bit-butil-mcp-tests")
            : configured;
    }

    private static string LocateDemoServerCsproj()
    {
        // Walk up from the test binaries rather than from the working directory: the CLI and the
        // IDE runners disagree about what the working directory is.
        var directory = AppContext.BaseDirectory;

        for (var i = 0; i < 10 && directory is not null; i++)
        {
            var candidate = Path.Combine(directory, "Bit.Butil.Demo", "Server", "Bit.Butil.Demo.Server.csproj");
            if (File.Exists(candidate)) return candidate;

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("Could not locate Bit.Butil.Demo.Server.csproj relative to the test binaries.");
    }

    private async Task WaitForReady(string baseUrl)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        // Long, because an un-warmed checkout builds the WebAssembly client before the host starts.
        var deadline = DateTime.UtcNow.AddMinutes(6);

        while (DateTime.UtcNow < deadline)
        {
            // A child that has already exited is never going to answer - fail now, with its exit
            // code, rather than after six minutes of polling a port nothing is listening on.
            if (_process is { HasExited: true })
                throw new InvalidOperationException($"The Bit.Butil demo server exited with code {_process.ExitCode} before it became ready. Its output is in the test log; set BUTIL_MCP_BASE_URL to run against a server you started yourself.");

            try
            {
                // robots.txt rather than "/": it is served by the same pipeline but renders no
                // components, so readiness does not wait on a first prerender.
                using var response = await http.GetAsync($"{baseUrl}/robots.txt");
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            catch (TaskCanceledException)
            {
                // Listening, but still starting up.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"The Bit.Butil demo server at {baseUrl} did not become ready in time. Set BUTIL_MCP_BASE_URL to run against a server you started yourself.");
    }
}
