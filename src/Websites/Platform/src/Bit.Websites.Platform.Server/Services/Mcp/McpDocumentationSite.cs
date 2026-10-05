using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Bit.Websites.Platform.Server.Services.Mcp;

/// <summary>
/// One bit library's documentation server, built out of a version's worktree and run on a loopback port so
/// <see cref="McpProxyService"/> can proxy that version's tools instead of the deployed, always-latest site.
/// </summary>
internal sealed partial class McpDocumentationSite : IDisposable
{
    /// <summary>The line Kestrel writes once it has bound, which is the only place the chosen port appears.</summary>
    [GeneratedRegex(@"Now listening on:\s*(?<url>http://\S+)")]
    private static partial Regex ListeningRegex();

    /// <summary>The libraries served per version, under the upstream names the proxy knows them by.</summary>
    public static readonly (string UpstreamName, string ProjectPath)[] Projects =
    [
        ("bitBlazorUI", "src/BlazorUI/Demo/Bit.BlazorUI.Demo.Server/Bit.BlazorUI.Demo.Server.csproj"),
        ("bitBrouter", "src/Brouter/Bit.Brouter.Demo/Server/Bit.Brouter.Demo.Server.csproj"),
        ("bitBswup", "src/Bswup/Bit.Bswup.Demo/Server/Bit.Bswup.Demo.Server.csproj"),
        ("bitButil", "src/Butil/Bit.Butil.Demo/Server/Bit.Butil.Demo.Server.csproj"),
        ("bitMotion", "src/Bmotion/Bit.Bmotion.Demo/Server/Bit.Bmotion.Demo.Server.csproj")
    ];

    private static readonly TimeSpan buildTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan startTimeout = TimeSpan.FromMinutes(5);

    private readonly ILogger logger;
    private readonly string projectFilePath;
    private readonly ConcurrentQueue<string> lastErrorLines = new();

    private string? assemblyPath;
    private Process? process;

    public McpDocumentationSite(string upstreamName, string versionName, string projectFilePath, ILogger logger)
    {
        UpstreamName = upstreamName;
        VersionName = versionName;
        this.projectFilePath = projectFilePath;
        this.logger = logger;
    }

    public string UpstreamName { get; }

    public string VersionName { get; }

    /// <summary>Where this site answers, known only once it has bound. It moves when the site restarts.</summary>
    public Uri? Endpoint { get; private set; }

    public bool IsRunning => process is { HasExited: false };

    /// <summary>
    /// Builds the project once, then starts it and waits until it answers. Debug on purpose: these projects
    /// only bundle their WebAssembly client, and turn on ReadyToRun and self-contained output, in Release.
    /// </summary>
    public async Task<bool> Build(CancellationToken cancellationToken)
    {
        if (assemblyPath is not null) return true;

        // Run from inside the worktree: the sdk global.json applies to the working directory, and an older
        // release pins an older sdk than the one this site itself is built with.
        var projectDirectory = Path.GetDirectoryName(projectFilePath);

        var target = await ProcessRunner.Run(ProcessRunner.For("dotnet", ["msbuild", projectFilePath, "-p:Configuration=Debug", "-getProperty:TargetPath", "-nologo"], projectDirectory), TimeSpan.FromMinutes(5), cancellationToken);

        if (target.Succeeded is false || string.IsNullOrWhiteSpace(target.Output))
        {
            logger.LogError("The assembly of {UpstreamName} {VersionName} could not be located: {Diagnostics}", UpstreamName, VersionName, Tail(target.Output + target.Error));
            return false;
        }

        var built = target.Output.Trim();
        // A worktree sits on a tag and never changes, so a build that finished once is still the right one.
        // The marker, rather than the assembly, is what says so: an interrupted build leaves one behind too.
        var marker = $"{built}.bitplatform-built";

        if (File.Exists(built) && File.Exists(marker))
        {
            assemblyPath = built;
            return true;
        }

        var build = await ProcessRunner.Run(ProcessRunner.For("dotnet", ["build", projectFilePath, "-c", "Debug", "--nologo", "-v", "minimal"], projectDirectory), buildTimeout, cancellationToken);

        if (build.Succeeded is false || File.Exists(built) is false)
        {
            logger.LogError("Building {UpstreamName} {VersionName} {Outcome}: {Diagnostics}",
                UpstreamName, VersionName, build.TimedOut ? $"hit its {buildTimeout.TotalMinutes:0} minute limit" : $"failed with {build.ExitCode}", Tail(build.Output + build.Error));
            return false;
        }

        File.WriteAllText(marker, VersionName);

        assemblyPath = built;

        return true;
    }

    /// <summary>Starts the site, or restarts it after it died. Returns whether it is answering.</summary>
    public async Task<bool> EnsureRunning(CancellationToken cancellationToken)
    {
        if (IsRunning) return true;

        if (process is not null)
        {
            logger.LogWarning("{UpstreamName} {VersionName} exited with {ExitCode}, restarting it: {Diagnostics}", UpstreamName, VersionName, process.ExitCode, string.Join('\n', lastErrorLines));
            Dispose();
        }

        if (await Build(cancellationToken) is false) return false;

        // Production, which keeps the scss watcher these demos start in Development out: it is a node process
        // per release watching a worktree that sits on a tag. Their UseHttpsRedirection stays inert either
        // way, since no https port is configured and none is bound.
        // The content root of an app run out of its build output is the working directory, as with dotnet run.
        var startInfo = ProcessRunner.For("dotnet", [assemblyPath!], Path.GetDirectoryName(projectFilePath));
        // Port 0, and the port it bound read back off its own startup line: reserving one here and handing it
        // over would lose the race against anything else on the machine taking it in between.
        startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";
        startInfo.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";

        // A child inherits this site's environment, and these carry an https port that is this site's, not its
        // own. UseHttpsRedirection would find one and answer every proxied call with a redirect off the loopback
        // port it was reached on. Dropped rather than blanked: the middleware parses what is there.
        foreach (var inherited in (string[])["ASPNETCORE_HTTPS_PORT", "HTTPS_PORT", "ASPNETCORE_HTTPS_PORTS", "ASPNETCORE_HTTP_PORTS", "ASPNETCORE_PORT"])
        {
            startInfo.Environment.Remove(inherited);
        }
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        lastErrorLines.Clear();

        TaskCompletionSource<Uri> listening = new(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Exception exp)
        {
            logger.LogError(exp, "{UpstreamName} {VersionName} could not be started.", UpstreamName, VersionName);
            return false;
        }

        ChildProcessJob.Adopt(process, logger);

        process.EnableRaisingEvents = true;
        process.Exited += (sender, e) => listening.TrySetCanceled();

        // Redirected pipes nobody reads fill up and block the child, so both are drained; only the last
        // few error lines are kept, for the log entry a crash writes.
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is null) return;

            logger.LogTrace("{UpstreamName} {VersionName}: {Line}", UpstreamName, VersionName, e.Data);

            if (ListeningRegex().Match(e.Data) is { Success: true } match)
            {
                listening.TrySetResult(new(new Uri(match.Groups["url"].Value), "/mcp"));
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is null) return;
            lastErrorLines.Enqueue(e.Data);
            while (lastErrorLines.Count > 20) lastErrorLines.TryDequeue(out _);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (await Bound(listening.Task, cancellationToken) && await WaitUntilAnswering(cancellationToken)) return true;

        logger.LogError("{UpstreamName} {VersionName} did not answer within {Timeout} minutes: {Diagnostics}",
            UpstreamName, VersionName, startTimeout.TotalMinutes, string.Join('\n', lastErrorLines));

        Dispose();

        return false;
    }

    private async Task<bool> Bound(Task<Uri> listening, CancellationToken cancellationToken)
    {
        using CancellationTokenSource delaySource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            if (await Task.WhenAny(listening, Task.Delay(startTimeout, delaySource.Token)) != listening) return false;
        }
        finally
        {
            await delaySource.CancelAsync();
        }

        // Cancelled rather than completed means the process exited before it ever bound.
        if (listening.IsCompletedSuccessfully is false) return false;

        Endpoint = listening.Result;

        return true;
    }

    private async Task<bool> WaitUntilAnswering(CancellationToken cancellationToken)
    {
        // Redirects are not followed: one means the app answers for somewhere other than the port it was
        // reached on, and following it would hide that behind a healthy looking 200 from another server.
        using HttpClientHandler handler = new() { AllowAutoRedirect = false };
        using HttpClient httpClient = new(handler) { Timeout = TimeSpan.FromSeconds(10), BaseAddress = new(Endpoint!.GetLeftPart(UriPartial.Authority)) };
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));

        var deadline = DateTimeOffset.UtcNow + startTimeout;

        while (DateTimeOffset.UtcNow < deadline && await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (process is null or { HasExited: true }) return false;

            try
            {
                using var response = await httpClient.GetAsync("/", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if ((int)response.StatusCode is >= 300 and < 400)
                {
                    logger.LogError("{UpstreamName} {VersionName} answers {Endpoint} with {StatusCode} to {Location} rather than serving it, so it is not usable.",
                        UpstreamName, VersionName, Endpoint, (int)response.StatusCode, response.Headers.Location);
                    return false;
                }

                // Anything else means Kestrel is listening and the pipeline ran; the proxy checks the tools
                // themselves when it lists them.
                return true;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested is false) { }
        }

        return false;
    }

    /// <summary>MSBuild failures are long; the end of the output is where the error is.</summary>
    private static string Tail(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join('\n', lines.TakeLast(20));
    }

    public void Dispose()
    {
        if (process is null) return;

        ProcessRunner.Kill(process);
        process.Dispose();
        process = null;
    }
}
