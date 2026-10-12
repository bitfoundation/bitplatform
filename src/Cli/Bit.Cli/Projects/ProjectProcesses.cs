using System.ComponentModel;
using System.Diagnostics;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Projects;

public static class ProjectProcesses
{
    public static async Task StopAsync(CliServices cli, StepRunner steps, string folder, CancellationToken cancellationToken)
    {
        await steps.RunAsync("build-server-shutdown", "Stopping the .NET build servers", async (progress, token) =>
        {
            var result = await cli.Runner.RunAsync(new ProcessSpec
            {
                FileName = "dotnet",
                Arguments = ["build-server", "shutdown"],
                WorkingDirectory = folder,
                OnOutputLine = progress,
                Timeout = TimeSpan.FromMinutes(2),
                Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1" }
            }, token);

            return result.Succeeded
                ? StepResult.Succeeded("Stopped the .NET build servers")
                : StepResult.Warning("Couldn't stop the .NET build servers", result.NotFound ? "dotnet wasn't found" : result.TimedOut ? "it took too long" : $"exit code {result.ExitCode}");
        }, cancellationToken);

        if (cli.Environment.IsWindows is false)
            return;

        var root = Path.GetFullPath(folder);
        var found = await cli.Console.RunWithStatusAsync("Looking for apps that use the project's files", _ => Task.Run(() => Find(root), cancellationToken));

        foreach (var (process, name, id) in found)
        {
            using (process)
            {
                var result = Stop(process, name, id);
                cli.Log.Write($"stop {name} ({id}): {result.Status} {result.Detail}");
                steps.Add("stop-process", result);
            }
        }
    }

    public static bool IsUnder(string path, string folder)
    {
        var root = folder.TrimEnd('\\', '/');

        return root.Length > 0
            && path.Length > root.Length + 1
            && path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            && path[root.Length] is '\\' or '/';
    }

    private static List<(Process Process, string Name, int Id)> Find(string folder)
    {
        var found = new List<(Process Process, string Name, int Id)>();

        foreach (var process in Process.GetProcesses())
        {
            if (process.Id != Environment.ProcessId && UsesFolder(process, folder) && TryGetName(process) is { } name)
            {
                found.Add((process, name, process.Id));
            }
            else
            {
                process.Dispose();
            }
        }

        return found;
    }

    private static bool UsesFolder(Process process, string folder)
    {
        try
        {
            if (process.MainModule?.FileName is { } main && IsUnder(main, folder))
                return true;

            return process.Modules.Cast<ProcessModule>().Any(module => module.FileName is { } file && IsUnder(file, folder));
        }
        catch (Exception exp) when (exp is Win32Exception or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    private static string? TryGetName(Process process)
    {
        try
        {
            return process.ProcessName;
        }
        catch (Exception exp) when (exp is InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    private static StepResult Stop(Process process, string name, int id)
    {
        try
        {
            process.Kill();
            process.WaitForExit(TimeSpan.FromSeconds(5));
            return StepResult.Succeeded($"Stopped {name}", $"process {id}");
        }
        catch (Exception exp) when (exp is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            return StepResult.Warning($"Couldn't stop {name}", $"process {id}: {exp.Message}");
        }
    }
}
