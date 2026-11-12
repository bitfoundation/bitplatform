using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Where the Workbench keeps its files on disk.</summary>
public static class WorkbenchFolders
{
    /// <summary>The folder of the Workbench's project file, found above the build output; null in a published copy. Tests point it at a copy of the project.</summary>
    public static string? Project { get; set; } = FindProject();

    /// <summary>
    /// The port it listens on. The Workbench every chat shares runs on the port of the project's launch settings; one a chat
    /// starts on another port, while that one is busy or down, keeps a review of its own, as two processes never share one.
    /// </summary>
    public static int? Port { get; set; }

    /// <summary>The src folder that holds the Workbench's project and the app's; null in a published copy.</summary>
    public static string? Source => Project is { } folder ? Path.GetDirectoryName(folder) : null;

    /// <summary>The root of the git working tree the Workbench runs from, which may be one of several worktrees of the project; null outside git.</summary>
    public static string? WorkingTree => FindGit()?.WorkingTree;

    /// <summary>The branch checked out in that working tree, or null when its HEAD isn't on one.</summary>
    public static string? Branch => FindGit() is { } git && ReadFile(Path.Combine(git.GitDir, "HEAD")) is { } head && head.StartsWith("ref: refs/heads/", StringComparison.Ordinal)
        ? head["ref: refs/heads/".Length..]
        : null;

    /// <summary>
    /// The review document, the page states saved with it and the invite key. Kept in this user's own folder, outside the
    /// repository, under the clone's shared git folder, so every worktree, branch and editor window of the project sees the
    /// same comments, flows and agents' tasks, and they outlive restarts and a switch to another worktree. A copy outside git,
    /// like a published one or a test's, keeps them next to its project, or its build output, in a folder git ignores.
    /// </summary>
    public static string Reviews => FindGit() is { } git && Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) is { Length: > 0 } userData
        ? Path.Combine(userData, "bit-workbench", ProjectKey(git.CommonDir, Path.GetRelativePath(git.WorkingTree, Project!)), ReviewsName)
        : Path.Combine(Project ?? AppContext.BaseDirectory, "App_Data", ReviewsName);

    /// <summary>The secrets the user pasted for an agent's commands to read, next to the reviews, outside the repository.</summary>
    public static string Secrets => Path.Combine(Path.GetDirectoryName(Reviews)!, "Secrets");

    /// <summary>The port of the first address in an ASP.NET Core urls setting, like http://localhost:6031.</summary>
    public static int? PortOf(string? urls)
    {
        return (urls ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                     .Select(url => Regex.Match(url, @":(\d+)/?$"))
                                     .FirstOrDefault(match => match.Success) is { } port ? int.Parse(port.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }

    private static string ReviewsName => Port is { } port && HomePort is { } home && port != home ? $"Reviews-{port}" : "Reviews";

    /// <summary>The port of the address in the project's launch settings.</summary>
    private static int? HomePort => Project is { } project
                                    && ReadFile(Path.Combine(project, "Properties", "launchSettings.json")) is { } settings
                                    && Regex.Match(settings, "\"applicationUrl\"\\s*:\\s*\"([^\"]*)\"") is { Success: true } url
                                    ? PortOf(url.Groups[1].Value)
                                    : null;

    private static string? FindProject()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Boilerplate.Workbench.csproj")))
                return folder.FullName;
        }

        return null;
    }

    /// <summary>
    /// The working tree above the project, its own git folder and the folder its worktrees share: a linked worktree's .git is a
    /// file naming its own folder, which names the shared one in its commondir file.
    /// </summary>
    private static (string WorkingTree, string GitDir, string CommonDir)? FindGit()
    {
        for (var folder = Project is null ? null : new DirectoryInfo(Project); folder is not null; folder = folder.Parent)
        {
            var dotGit = Path.Combine(folder.FullName, ".git");

            if (Directory.Exists(dotGit))
                return (folder.FullName, dotGit, dotGit);

            if (ReadFile(dotGit) is { } pointer && pointer.StartsWith("gitdir:", StringComparison.Ordinal))
            {
                var gitDir = Path.GetFullPath(pointer["gitdir:".Length..].Trim(), folder.FullName);
                var commonDir = ReadFile(Path.Combine(gitDir, "commondir")) is { } common ? Path.GetFullPath(common, gitDir) : gitDir;

                return (folder.FullName, gitDir, commonDir);
            }
        }

        return null;
    }

    /// <summary>
    /// The name of the clone's main folder with a short hash of where it is and of where the Workbench sits in it, so two clones
    /// of one repository, or two projects in one, stay apart, while every worktree of a project, which has it in the same place,
    /// shares one. test-platform.ps1's Get-WorkbenchDataFolder names it the same way.
    /// </summary>
    private static string ProjectKey(string commonDir, string projectInTree)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(commonDir));
        var name = Path.GetFileName(full) is ".git" ? Path.GetFileName(Path.GetDirectoryName(full)) : Path.GetFileName(full);
        var key = $"{full}|{projectInTree.Replace('\\', '/')}";
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(OperatingSystem.IsWindows() ? key.ToLowerInvariant() : key)))[..12];
        var safe = new string([.. (name ?? "project").Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-')]);

        return $"{safe}-{hash}";
    }

    private static string? ReadFile(string path) => File.Exists(path) ? File.ReadAllText(path).Trim() : null;
}
