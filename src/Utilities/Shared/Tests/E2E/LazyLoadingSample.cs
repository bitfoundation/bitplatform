using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Bit.Utilities.Testing;

/// <summary>
/// A library's lazy loading sample (<c>Samples/Lazy/Bit.&lt;Lib&gt;.Samples.Lazy</c>) as <c>dotnet publish</c> left it,
/// running for the end-to-end suite: a Blazor Web App whose router runs on WebAssembly and downloads the library only
/// when a page that uses it opens, set up as the library's guide says - its client trimmed, the library marked
/// <c>BlazorWebAssemblyLazyLoad</c> and none of its services registered in the browser. Only a publish trims the client,
/// fingerprints the assembly and writes the lazy mark into the boot configuration, so a build output proves none of it.
/// Linked into the end-to-end project of every library whose guide says it lazy loads, so one fix here reaches all of
/// them; see <c>src/Utilities/Shared/LazyLoading.md</c>.
/// </summary>
/// <remarks>
/// Started by the first test that drives it and shared by the rest of the run, as the demo is: it keeps no state on
/// the server, and every test opens a browser context of its own. The environment the suite reads, under the library's
/// prefix (<c>BDND</c> for <c>BDND_E2E_LAZY_BASE_URL</c>):
/// <list type="bullet">
/// <item><c>&lt;PREFIX&gt;_E2E_LAZY_BASE_URL</c>: an already running sample, driven instead of the published one.</item>
/// <item><c>&lt;PREFIX&gt;_E2E_LAZY_AOT=1</c>: the sample was published with <c>-p:RunAOTCompilation=true</c>, which leaves the
/// lazy mark out as the guide says an AOT build must, so the library is expected to load with the app.</item>
/// </list>
/// </remarks>
internal sealed class LazyLoadingSample : IAsyncDisposable
{
    private static readonly SemaphoreSlim _gate = new(1, 1);
    private static Task<LazyLoadingSample>? _started;

    private readonly Process? _process;
    private readonly ConcurrentQueue<string> _output = new();

    private LazyLoadingSample(string library, string baseUrl, bool lazyLoads, Process? process)
    {
        Library = library;
        BaseUrl = baseUrl;
        LazyLoads = lazyLoads;
        _process = process;
    }

    /// <summary>The library the sample lazy loads, by its assembly name: <c>Bit.Bdnd</c>.</summary>
    public string Library { get; }

    /// <summary>The origin the sample answers at, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Whether the sample was published to download the library on demand: false for an AOT publish, which loads it with
    /// the rest of the app, as the guide says an AOT build must.
    /// </summary>
    public bool LazyLoads { get; }

    /// <summary>
    /// The sample of <paramref name="library"/>, started once for the run: from its publish folder
    /// (<c>bin/&lt;configuration&gt;/net11.0/publish</c>, where <c>dotnet publish -c &lt;configuration&gt;</c> leaves it) in the
    /// Production environment, or the one <c>&lt;PREFIX&gt;_E2E_LAZY_BASE_URL</c> names. A sample that is not published or does
    /// not start fails the first test with <see cref="FileNotFoundException"/>, <see cref="InvalidOperationException"/> or
    /// <see cref="TimeoutException"/>, and every later test with the same failure at once.
    /// </summary>
    /// <param name="library">The library's assembly name, <c>Bit.Bdnd</c>; the sample is <c>Bit.Bdnd.Samples.Lazy</c>.</param>
    /// <param name="prefix">The prefix of the suite's environment variables, <c>BDND</c>.</param>
    /// <param name="configuration">The configuration the sample was published in.</param>
    public static async Task<LazyLoadingSample> GetAsync(string library, string prefix, string configuration)
    {
        await _gate.WaitAsync();
        try
        {
            _started ??= StartAsync(library, prefix, configuration);
        }
        finally
        {
            _gate.Release();
        }

        return await _started;
    }

    /// <summary>Stops the sample the run started, if it started one. Called from the suite's assembly cleanup.</summary>
    public static async Task StopAsync()
    {
        if (_started is { IsCompletedSuccessfully: true } started) await started.Result.DisposeAsync();
    }

    /// <summary>
    /// Counts the requests <paramref name="page"/> makes for the sample's lazily loaded assemblies from now on -
    /// <see cref="Library"/> and <paramref name="others"/> it is loaded together with - at their fingerprinted address,
    /// <c>_framework/Bit.Bdnd.&lt;hash&gt;.wasm</c>. Call it before the page's first navigation.
    /// </summary>
    public LazyAssemblyRequests Track(IPage page, params string[] others) => new(page, [Library, .. others]);

    /// <summary>
    /// The start page, which names none of the library's types, has run in the browser: the library must not have been
    /// downloaded. In an AOT publish it comes with the app, so nothing is asserted beyond what <see cref="AssertLoadedOnce"/>
    /// checks later.
    /// </summary>
    public void AssertNotLoaded(LazyAssemblyRequests requests)
    {
        requests.MarkStart();
        if (LazyLoads is false) return;

        foreach (var assembly in requests.Assemblies)
            Assert.AreEqual(0, requests.Count(assembly), $"the start page uses nothing of {assembly}, yet it was downloaded:{Environment.NewLine}{requests}");
    }

    /// <summary>
    /// A page that uses the library has rendered: the router downloaded each lazily loaded assembly exactly once, however
    /// many times its <c>OnNavigateAsync</c> asked for it - or, in an AOT publish, nothing was downloaded on demand at all.
    /// </summary>
    public void AssertLoadedOnce(LazyAssemblyRequests requests)
    {
        foreach (var assembly in requests.Assemblies)
        {
            if (LazyLoads)
                Assert.AreEqual(1, requests.Count(assembly), $"{assembly} should have been downloaded once, when the first page that uses it opened:{Environment.NewLine}{requests}");
            else if (requests.CountAtStart(assembly) is { } atStart)
                Assert.AreEqual(atStart, requests.Count(assembly), $"an AOT build loads {assembly} with the app, yet it was downloaded again on demand:{Environment.NewLine}{requests}");
            else
                // Opened at a page that uses it, there is no start page to compare with: an AOT app downloads the
                // assembly's metadata as it boots, once, or not at all.
                Assert.IsLessThanOrEqualTo(1, requests.Count(assembly), $"an AOT build loads {assembly} with the app, yet it was downloaded more than once:{Environment.NewLine}{requests}");
        }
    }

    private static async Task<LazyLoadingSample> StartAsync(string library, string prefix, string configuration)
    {
        var lazyLoads = Read($"{prefix}_E2E_LAZY_AOT") != "1";

        if (Read($"{prefix}_E2E_LAZY_BASE_URL") is { } external)
            return new LazyLoadingSample(library, external.TrimEnd('/'), lazyLoads, process: null);

        var name = $"{library}.Samples.Lazy";
        var project = FindUpward(Path.Combine("Samples", "Lazy", name, $"{name}.csproj"));
        var directory = Path.Combine(Path.GetDirectoryName(project)!, "bin", configuration, "net11.0", "publish");
        var assembly = Path.Combine(directory, $"{name}.dll");
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException(
                $"The lazy loading sample has not been published in the {configuration} configuration. Publish it first with " +
                $"'dotnet publish {project} -c {configuration}'", assembly);

        var baseUrl = $"http://127.0.0.1:{FreePort()}";
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = directory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(assembly);
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(baseUrl);
        // A publish output serves its static web assets from its own wwwroot, as it does in production.
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        var sample = new LazyLoadingSample(library, baseUrl, lazyLoads, new Process { StartInfo = startInfo });
        sample._process!.OutputDataReceived += (_, e) => sample.Keep(e.Data);
        sample._process.ErrorDataReceived += (_, e) => sample.Keep(e.Data);
        if (sample._process.Start() is false) throw new InvalidOperationException($"Failed to start {assembly}.");
        sample._process.BeginOutputReadLine();
        sample._process.BeginErrorReadLine();

        try
        {
            await sample.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
        }
        catch
        {
            await sample.DisposeAsync();
            throw;
        }

        return sample;
    }

    private async Task WaitForHttpAsync(string url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (_process!.HasExited)
                throw new InvalidOperationException($"The lazy loading sample exited before {url} answered.{Environment.NewLine}{string.Join(Environment.NewLine, _output)}");

            try
            {
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { /* not listening yet */ }
            catch (TaskCanceledException) { /* not answering yet */ }

            await Task.Delay(250);
        }

        throw new TimeoutException($"{url} did not answer within {timeout}.{Environment.NewLine}{string.Join(Environment.NewLine, _output)}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is null) return;

        try
        {
            if (_process.HasExited is false)
            {
                _process.Kill(entireProcessTree: true);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _process.WaitForExitAsync(cts.Token);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or System.ComponentModel.Win32Exception)
        {
            // Best-effort: the process may already be gone, or refuse to die before the run ends.
        }
        finally
        {
            _process.Dispose();
        }
    }

    // Drained, or a full pipe would stall the server; the last lines go into the message of a sample that fails to start.
    private void Keep(string? line)
    {
        if (line is null) return;

        _output.Enqueue(line);
        while (_output.Count > 200) _output.TryDequeue(out _);
    }

    private static int FreePort()
    {
        // Bind to port 0 and release it: the OS hands back a free ephemeral port.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    // From the test binary's location, so a run does not depend on the working directory the shell, the IDE or the test
    // runner picked.
    private static string FindUpward(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException($"Could not find {relativePath} walking up from {AppContext.BaseDirectory}.");
    }

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

/// <summary>
/// The requests a page made for a set of assemblies at the address the WebAssembly runtime fetches them from:
/// <c>_framework/Bit.Bdnd.wasm</c>, or, once published, <c>_framework/Bit.Bdnd.&lt;hash&gt;.wasm</c>. Counted as the browser
/// sends them, over every document the page loads, so a download repeated on a later navigation is counted too.
/// </summary>
internal sealed class LazyAssemblyRequests
{
    private readonly Dictionary<string, Regex> _patterns;
    private readonly List<string> _urls = [];
    private Dictionary<string, int>? _atStart;

    public LazyAssemblyRequests(IPage page, IEnumerable<string> assemblies)
    {
        // The fingerprint is lower case letters and digits, so Bit.Bmage does not match Bit.Bmage.Blazor.wasm.
        _patterns = assemblies.Distinct().ToDictionary(assembly => assembly,
            assembly => new Regex($@"^/_framework/{Regex.Escape(assembly)}(\.[a-z0-9]+)?\.wasm$", RegexOptions.CultureInvariant));

        page.Request += (_, request) =>
        {
            if (Uri.TryCreate(request.Url, UriKind.Absolute, out var url) is false) return;
            if (_patterns.Values.Any(pattern => pattern.IsMatch(url.AbsolutePath)) is false) return;
            lock (_urls) _urls.Add(url.AbsolutePath);
        };
    }

    /// <summary>The assemblies counted.</summary>
    public IReadOnlyCollection<string> Assemblies => _patterns.Keys;

    /// <summary>The requests made for <paramref name="assembly"/> so far.</summary>
    public int Count(string assembly)
    {
        lock (_urls) return _urls.Count(url => _patterns[assembly].IsMatch(url));
    }

    // What was downloaded by the time the start page ran, or null when the page was opened at one that uses them.
    internal int? CountAtStart(string assembly) => _atStart?.GetValueOrDefault(assembly);

    internal void MarkStart() => _atStart = _patterns.Keys.ToDictionary(assembly => assembly, Count);

    public override string ToString()
    {
        lock (_urls) return _urls.Count == 0 ? "(no request for them)" : string.Join(Environment.NewLine, _urls);
    }
}
