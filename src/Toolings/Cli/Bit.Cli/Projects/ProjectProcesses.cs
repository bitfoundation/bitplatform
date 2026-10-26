using System.ComponentModel;
using System.Diagnostics;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Projects;

public sealed record FreedFiles(IReadOnlyList<string> Stopped, IReadOnlyList<string> Holders)
{
    public static FreedFiles None { get; } = new([], []);

    public string? InUseBy => Holders.Count == 0 ? null : $"In use by {string.Join(", ", Holders)}";
}

public static class ProjectProcesses
{
    private static readonly HashSet<string> buildProcesses = new(["dotnet", "MSBuild", "VBCSCompiler"], StringComparer.OrdinalIgnoreCase);

    public static async Task StopAsync(CliServices cli, StepRunner steps, string folder, CancellationToken cancellationToken)
    {
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

    public static FreedFiles Free(CliServices cli, IEnumerable<string> paths, bool stopBuildProcesses = true)
    {
        if (cli.Environment.IsWindows is false)
            return FreedFiles.None;

        return Free(cli, OperatingSystem.IsWindows() ? FileHolders.Find(paths) : [], stopBuildProcesses, "it held the project's files");
    }

    public static FreedFiles FreeFolder(CliServices cli, string folder)
    {
        if (cli.Environment.IsWindows is false)
            return FreedFiles.None;

        return Free(cli, OperatingSystem.IsWindows() ? FolderHolders.Find(folder) : [], stopBuildProcesses: true, "it was in the project's folder");
    }

    private static FreedFiles Free(CliServices cli, IReadOnlyList<int> ids, bool stopBuildProcesses, string reason)
    {
        var stopped = new List<string>();
        var holders = new List<string>();

        foreach (var id in ids)
        {
            if (id == Environment.ProcessId || TryGetProcess(id) is not { } process)
                continue;

            using (process)
            {
                if (TryGetName(process) is not { } name)
                    continue;

                if (stopBuildProcesses && buildProcesses.Contains(name) && Stop(process, name, id) is { Status: StepStatus.Succeeded })
                {
                    cli.Log.Write($"stop {name} ({id}): {reason}");
                    stopped.Add($"{name} ({id})");
                }
                else
                {
                    holders.Add($"{name} ({id})");
                }
            }
        }

        return new(stopped, holders);
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

    private static Process? TryGetProcess(int id)
    {
        try
        {
            return Process.GetProcessById(id);
        }
        catch (Exception exp) when (exp is ArgumentException or InvalidOperationException)
        {
            return null;
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
