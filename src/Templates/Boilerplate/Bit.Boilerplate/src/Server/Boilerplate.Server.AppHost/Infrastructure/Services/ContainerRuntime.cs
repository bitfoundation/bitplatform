using System.Collections.Concurrent;
using System.ComponentModel;

namespace Boilerplate.Server.AppHost.Infrastructure.Services;

/// <summary>Picks the container runtime the way Aspire does, as a full path, since the Windows keeper doesn't inherit PATH.</summary>
internal static class ContainerRuntime
{
    private static readonly ConcurrentDictionary<string, (string Path, bool Running)> found = [];

    public static string Find(IDistributedApplicationBuilder builder) => Resolve(builder).Path;

    /// <summary>False only when the runtime is surely missing or stopped; a slow answer counts as running.</summary>
    public static bool IsRunning(IDistributedApplicationBuilder builder) => Resolve(builder).Running;

    private static (string Path, bool Running) Resolve(IDistributedApplicationBuilder builder)
    {
        var configured = builder.Configuration["DcpPublisher:ContainerRuntime"]
            ?? builder.Configuration["ASPIRE_CONTAINER_RUNTIME"]
            ?? builder.Configuration["DOTNET_ASPIRE_CONTAINER_RUNTIME"]
            ?? "";

        return found.GetOrAdd(configured, static configured =>
        {
            if (configured.Length > 0)
                return Check(configured);

            var docker = Check("docker");

            if (docker.Running)
                return docker;

            var podman = Check("podman");

            return podman.Running || (File.Exists(docker.Path) is false && File.Exists(podman.Path)) ? podman : docker;
        });
    }

    private static (string Path, bool Running) Check(string name)
    {
        var path = FindExecutable(name);

        try
        {
            using var process = Process.Start(new ProcessStartInfo(path, ["container", "ls", "-n", "1"])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
                return (path, false);

            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            if (process.WaitForExit(TimeSpan.FromSeconds(20)))
                return (path, process.ExitCode is 0);

            process.Kill(entireProcessTree: true);
            return (path, true);
        }
        catch (Win32Exception)
        {
            return (path, false);
        }
    }

    private static string FindExecutable(string name)
        => (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, OperatingSystem.IsWindows() ? $"{name}.exe" : name))
            .FirstOrDefault(File.Exists) ?? name;
}
