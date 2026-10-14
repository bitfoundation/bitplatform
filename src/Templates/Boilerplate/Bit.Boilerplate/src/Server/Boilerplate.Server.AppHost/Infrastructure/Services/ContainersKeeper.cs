using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Server.AppHost.Infrastructure.Services;

/// <summary>
/// Stops the app host's persistent containers once no run of this app host has been alive for a while.
/// <para>
/// The first run starts a tiny keeper process (PowerShell on Windows, <c>/bin/sh</c> elsewhere) outside the app host's
/// process tree, and every later run reuses it. While an app host runs, this hosted service writes a <c>heartbeat</c>
/// file every 10 seconds and records the ids of its containers in a <c>containers</c> file. The keeper deletes the
/// heartbeat every 30 seconds; once it hasn't seen one for the idle timeout, it runs <c>docker stop</c> on the recorded
/// containers and exits. The next run starts a new keeper, and Aspire re-creates the stopped containers.
/// </para>
/// <para>
/// The state lives in <c>%TEMP%/aspire-containers-keeper/&lt;app host id&gt;</c>: <c>process</c> (the keeper's pid and start
/// time), <c>heartbeat</c>, <c>containers</c>, and <c>stopping</c> while the keeper is stopping the containers.
/// </para>
/// <para>
/// Aspire's own <c>WithParentProcessLifetime</c> isn't used: its DCP monitors live in the kill-on-close job that
/// <c>dotnet run</c> / <c>dotnet watch</c> put the app host in, so they die together with the run.
/// </para>
/// </summary>
internal sealed class ContainersKeeper(string stateDirectory, ResourceNotificationService resourceNotifications) : BackgroundService
{
    // How often the keeper looks for a heartbeat. It must stay well above heartbeatInterval, so a running app host never looks idle.
    private const int CheckIntervalSeconds = 30;

    private static readonly TimeSpan heartbeatInterval = TimeSpan.FromSeconds(10);

    // $1 = state directory, $2 = idle seconds, $3 = check interval seconds, $4 = container runtime (docker or podman).
    private const string UnixKeeperScript = """
        cd "$1" || exit 1
        idle=0
        while :; do
            sleep "$3"
            if [ -e heartbeat ]; then rm -f heartbeat; idle=0; continue; fi
            idle=$((idle + $3))
            [ "$idle" -lt "$2" ] && continue
            : > stopping
            if [ -e heartbeat ]; then rm -f stopping; idle=0; continue; fi
            [ -s containers ] && "$4" stop $(cat containers) > /dev/null 2>&1
            rm -f process containers stopping
            exit 0
        done
        """;

    /// <summary>
    /// Makes sure a keeper is running for this app host and registers the hosted service that keeps it alive.
    /// Runs while the application model is built, before any container is created.
    /// </summary>
    public static void Start(IDistributedApplicationBuilder builder, TimeSpan idleTimeout)
    {
        var stateDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "aspire-containers-keeper", GetAppHostId(builder.AppHostDirectory))).FullName;

        // App hosts of the same project may start together (e.g. aspire start and dotnet test), and only one of them may start a keeper.
        using var mutex = new Mutex(initiallyOwned: false, $"aspire-containers-keeper-{Path.GetFileName(stateDirectory)}");
        var acquired = Acquire(mutex);

        try
        {
            // Beat before looking for the "stopping" marker: either the keeper sees this heartbeat and backs off,
            // or this run sees the marker and waits until the keeper has stopped the containers.
            Beat(stateDirectory);
            WaitWhileStopping(stateDirectory);

            // If no keeper can be started, the containers are still persistent; they just never stop on their own.
            if (IsKeeperRunning(stateDirectory) || StartKeeper(builder, stateDirectory, idleTimeout))
            {
                builder.Services.AddHostedService(services => new ContainersKeeper(stateDirectory, services.GetRequiredService<ResourceNotificationService>()));
            }
        }
        finally
        {
            if (acquired)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.WhenAll(KeepAlive(stoppingToken), RecordContainers(stoppingToken));

    private async Task KeepAlive(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(heartbeatInterval);

        do
        {
            Beat(stateDirectory);
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    // Ids come from Aspire's resource notifications, so a container started later from the dashboard (explicit start) is
    // recorded too. A container re-created by a later run gets a new id; the old one is harmless, docker stop skips it.
    private async Task RecordContainers(CancellationToken cancellationToken)
    {
        var containersPath = Path.Combine(stateDirectory, "containers");

        await foreach (var resourceEvent in resourceNotifications.WatchAsync(cancellationToken))
        {
            if (resourceEvent.Resource is not ContainerResource
                || resourceEvent.Snapshot.Properties.FirstOrDefault(property => property.Name is "container.id")?.Value is not string { Length: > 0 } containerId)
                continue;

            try
            {
                if (File.Exists(containersPath) is false || File.ReadLines(containersPath).Contains(containerId) is false)
                {
                    File.AppendAllLines(containersPath, [containerId]);
                }
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static bool StartKeeper(IDistributedApplicationBuilder builder, string stateDirectory, TimeSpan idleTimeout)
    {
        try
        {
            // A new keeper starts with an empty list; containers that outlived a killed keeper are recorded again by the run that adopts them.
            File.Delete(Path.Combine(stateDirectory, "containers"));

            // Same lookup order as Aspire. The full path, because the Windows keeper doesn't inherit the app host's PATH.
            var containerRuntime = FindExecutable(builder.Configuration["DcpPublisher:ContainerRuntime"]
                ?? builder.Configuration["ASPIRE_CONTAINER_RUNTIME"]
                ?? builder.Configuration["DOTNET_ASPIRE_CONTAINER_RUNTIME"]
                ?? "docker");

            var processId = OperatingSystem.IsWindows()
                ? StartWindowsKeeper(stateDirectory, (int)idleTimeout.TotalSeconds, containerRuntime)
                : StartUnixKeeper(GetDcpPath(builder), stateDirectory, (int)idleTimeout.TotalSeconds, containerRuntime);

            if (processId is null)
                return false;

            File.WriteAllText(Path.Combine(stateDirectory, "process"), $"{processId} {GetStartTicks(processId.Value)}");

            return true;
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }

    // dotnet run / dotnet watch keep the app host in a job object that kills every process in it when the run ends, and
    // breaking away from it isn't allowed. A process created through WMI (Win32_Process.Create) is started by the WMI
    // service instead, so it belongs to no job and outlives the run. The launcher itself only prints the keeper's pid.
    private static int? StartWindowsKeeper(string stateDirectory, int idleSeconds, string containerRuntime)
    {
        var powerShell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");

        // The same loop as UnixKeeperScript.
        var keeperScript = $$"""
            Set-Location -LiteralPath '{{Quote(stateDirectory)}}'
            $idle = 0
            while ($true) {
                Start-Sleep -Seconds {{CheckIntervalSeconds}}
                if (Test-Path heartbeat) { Remove-Item heartbeat -Force -ErrorAction SilentlyContinue; $idle = 0; continue }
                $idle += {{CheckIntervalSeconds}}
                if ($idle -lt {{idleSeconds}}) { continue }
                New-Item stopping -ItemType File -Force | Out-Null
                if (Test-Path heartbeat) { Remove-Item stopping -Force -ErrorAction SilentlyContinue; $idle = 0; continue }
                if (Test-Path containers) { & '{{Quote(containerRuntime)}}' stop @(Get-Content containers) *> $null }
                Remove-Item process, containers, stopping -Force -ErrorAction SilentlyContinue
                break
            }
            """;

        var launcherScript = $$"""
            $ProgressPreference = 'SilentlyContinue'
            $startup = New-CimInstance -ClassName Win32_ProcessStartup -ClientOnly -Property @{ ShowWindow = [uint16]0 }
            $result = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = '"{{Quote(powerShell)}}" -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {{Encode(keeperScript)}}'; CurrentDirectory = '{{Quote(stateDirectory)}}'; ProcessStartupInformation = $startup }
            if ($result.ReturnValue -eq 0) { $result.ProcessId }
            """;

        return RunForProcessId(powerShell, ["-NoProfile", "-NonInteractive", "-EncodedCommand", Encode(launcherScript)]);

        static string Quote(string value) => value.Replace("'", "''");

        static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }

    // dcp fork-process (shipped with Aspire) starts the keeper in a new session (setsid), outside the app host's process
    // group, and prints its pid. The line endings are normalized because a CRLF checkout would break the script.
    private static int? StartUnixKeeper(string? dcpPath, string stateDirectory, int idleSeconds, string containerRuntime)
        => File.Exists(dcpPath)
            ? RunForProcessId(dcpPath, ["fork-process", "--", "/bin/sh", "-c", UnixKeeperScript.ReplaceLineEndings("\n"), "containers-keeper", stateDirectory, $"{idleSeconds}", $"{CheckIntervalSeconds}", containerRuntime])
            : null;

    private static int? RunForProcessId(string fileName, IEnumerable<string> arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
            return null;

        _ = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode is 0 && int.TryParse(output.Trim(), out var processId) ? processId : null;
    }

    // The start time guards against the pid having been reused by an unrelated process.
    private static bool IsKeeperRunning(string stateDirectory)
    {
        var processPath = Path.Combine(stateDirectory, "process");

        try
        {
            if (File.Exists(processPath) is false)
                return false;

            var parts = File.ReadAllText(processPath).Split(' ');

            return parts.Length is 2
                && int.TryParse(parts[0], out var processId)
                && long.TryParse(parts[1], out var startTicks)
                && Math.Abs(GetStartTicks(processId) - startTicks) < TimeSpan.TicksPerSecond;
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }

    // Waits only while the keeper is alive, so a keeper killed in the middle of stopping can't hold up every later run.
    private static void WaitWhileStopping(string stateDirectory)
    {
        var stoppingPath = Path.Combine(stateDirectory, "stopping");
        var deadline = DateTime.UtcNow.AddMinutes(2);

        while (File.Exists(stoppingPath) && IsKeeperRunning(stateDirectory) && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(500);
        }

        File.Delete(stoppingPath);
    }

    private static string FindExecutable(string name)
        => (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, OperatingSystem.IsWindows() ? $"{name}.exe" : name))
            .FirstOrDefault(File.Exists) ?? name;

    // Same lookup order as Aspire's own DcpOptions.
    private static string? GetDcpPath(IDistributedApplicationBuilder builder)
    {
        if (builder.Configuration["DcpPublisher:CliPath"] is { Length: > 0 } cliPath)
            return cliPath;

        if (builder.Configuration["ASPIRE_DCP_PATH"] is { Length: > 0 } dcpDirectory)
            return Path.Combine(dcpDirectory, OperatingSystem.IsWindows() ? "dcp.exe" : "dcp");

        return typeof(ContainersKeeper).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(metadata => string.Equals(metadata.Key, "DcpCliPath", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    // One state directory per app host path, so different projects (or worktrees of one project) don't share a keeper.
    private static string GetAppHostId(string appHostDirectory)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Path.TrimEndingDirectorySeparator(Path.GetFullPath(appHostDirectory)).ToUpperInvariant())))[..16];

    private static long GetStartTicks(int processId)
    {
        using var process = Process.GetProcessById(processId);

        return process.StartTime.ToUniversalTime().Ticks;
    }

    private static bool Acquire(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(TimeSpan.FromSeconds(30));
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    // The keeper deletes this file on every check, so its existence means an app host was alive within the last check interval.
    private static void Beat(string stateDirectory)
    {
        try
        {
            File.WriteAllBytes(Path.Combine(stateDirectory, "heartbeat"), []);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }
    }
}
