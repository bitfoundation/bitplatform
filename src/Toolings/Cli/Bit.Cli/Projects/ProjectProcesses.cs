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
    private static readonly HashSet<string> buildProcesses = new(["dotnet", "MSBuild", "VBCSCompiler", "XamlCompiler"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> projectHosts = new([.. buildProcesses, "aspire"], StringComparer.OrdinalIgnoreCase);

    public static async Task<IReadOnlySet<int>> StopAsync(CliServices cli, StepRunner steps, string folder, CancellationToken cancellationToken)
    {
        var stoppedBuilds = new HashSet<int>();

        if (cli.Environment.IsWindows is false)
            return stoppedBuilds;

        var root = Path.GetFullPath(folder);
        var found = await cli.Console.RunWithStatusAsync("Looking for apps that use the project's files", _ => Task.Run(() => Find(root), cancellationToken));

        foreach (var (process, name, id) in found)
        {
            using (process)
            {
                var result = Stop(process, name, id);
                cli.Log.Write($"stop {name} ({id}): {result.Status} {result.Detail}");
                steps.Add("stop-process", result);

                if (result.Status is StepStatus.Succeeded && buildProcesses.Contains(name))
                {
                    stoppedBuilds.Add(id);
                }
            }
        }

        return stoppedBuilds;
    }

    public static FreedFiles Free(CliServices cli, IEnumerable<string> paths, bool stopBuildProcesses = true, IReadOnlySet<int>? stoppedBuilds = null)
    {
        if (cli.Environment.IsWindows is false)
            return FreedFiles.None;

        return Free(cli, OperatingSystem.IsWindows() ? FileHolders.Find(paths) : [], stopBuildProcesses, stoppedBuilds, "it held the project's files");
    }

    public static FreedFiles FreeFolder(CliServices cli, string folder, IReadOnlySet<int>? stoppedBuilds = null)
    {
        if (cli.Environment.IsWindows is false)
            return FreedFiles.None;

        return Free(cli, OperatingSystem.IsWindows() ? FolderHolders.Find(folder) : [], stopBuildProcesses: true, stoppedBuilds, "it was in the project's folder");
    }

    private static FreedFiles Free(CliServices cli, IReadOnlyList<int> ids, bool stopBuildProcesses, IReadOnlySet<int>? stoppedBuilds, string reason)
    {
        var stopped = new List<string>();
        var holders = new List<string>();
        var found = new List<(Process Process, string Name, int Id, bool Build)>();
        var ancestors = Ancestors();

        foreach (var id in ids)
        {
            if (id == Environment.ProcessId || ancestors.Contains(id) || TryGetProcess(id) is not { } process)
                continue;

            if (TryGetName(process) is { } name)
            {
                found.Add((process, name, id, stopBuildProcesses && (buildProcesses.Contains(name) || IsStartedByABuild(id, stoppedBuilds))));
            }
            else
            {
                process.Dispose();
            }
        }

        foreach (var (process, name, id, build) in found)
        {
            using (process)
            {
                if (build && Stop(process, name, id) is { Status: StepStatus.Succeeded })
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

    private static bool IsStartedByABuild(int id, IReadOnlySet<int>? stoppedBuilds)
    {
        if (!OperatingSystem.IsWindows() || FolderHolders.ParentOf(id) is not { } parentId)
            return false;

        if (stoppedBuilds?.Contains(parentId) is true)
            return true;

        if (TryGetProcess(parentId) is not { } parent)
            return false;

        using (parent)
        {
            return TryGetName(parent) is { } name && buildProcesses.Contains(name);
        }
    }

    private static HashSet<int> Ancestors()
    {
        var ids = new HashSet<int>();
        var id = Environment.ProcessId;

        while (OperatingSystem.IsWindows() && FolderHolders.ParentOf(id) is { } parent && ids.Add(parent))
        {
            id = parent;
        }

        return ids;
    }

    public static bool IsUnder(string path, string folder)
    {
        var root = folder.TrimEnd('\\', '/');

        return root.Length > 0
            && path.Length > root.Length + 1
            && path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            && path[root.Length] is '\\' or '/';
    }

    public static bool Mentions(string commandLine, string folder)
    {
        var root = folder.TrimEnd('\\', '/');

        for (var at = commandLine.IndexOf(root, StringComparison.OrdinalIgnoreCase); root.Length > 0 && at >= 0; at = commandLine.IndexOf(root, at + 1, StringComparison.OrdinalIgnoreCase))
        {
            var end = at + root.Length;

            if (end == commandLine.Length || commandLine[end] is '\\' or '/' or '"' or ' ')
                return true;
        }

        return false;
    }

    private static List<(Process Process, string Name, int Id)> Find(string folder)
    {
        var found = new List<(Process Process, string Name, int Id)>();
        var ancestors = Ancestors();

        foreach (var process in Process.GetProcesses())
        {
            if (process.Id != Environment.ProcessId && ancestors.Contains(process.Id) is false && TryGetName(process) is { } name && (UsesFolder(process, folder) || RunsTheProject(process.Id, name, folder)))
            {
                found.Add((process, name, process.Id));
            }
            else
            {
                process.Dispose();
            }
        }

        return [.. found.OrderByDescending(f => projectHosts.Contains(f.Name))];
    }

    private static bool RunsTheProject(int id, string name, string folder)
    {
        return OperatingSystem.IsWindows() && projectHosts.Contains(name) && FolderHolders.Runs(id, folder);
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
