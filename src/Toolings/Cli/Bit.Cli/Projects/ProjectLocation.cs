using Bit.Cli.Infrastructure;

namespace Bit.Cli.Projects;

public static class ProjectLocation
{
    private static readonly string[] WindowsSystemVariables = ["SystemRoot", "windir", "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432", "ProgramData"];

    private static readonly string[] UnixSystemFolders = ["/bin", "/boot", "/dev", "/etc", "/lib", "/lib32", "/lib64", "/libx32", "/proc", "/run", "/sbin", "/sys", "/usr", "/Applications", "/Library", "/System"];

    public static string ReposDirectory(CliEnvironment environment) => Path.Combine(environment.HomeDirectory, "source", "repos");

    public static (string Directory, string? Reason) Choose(CliEnvironment environment, string directory, string name)
    {
        var reason = IsSystemFolder(environment, directory)
            ? $"{Path.GetDirectoryName(directory) ?? directory} is a system folder"
            : NearestExisting(directory) is { } existing && AdministratorNeeds.CanWrite(existing) is false
                ? $"bit can't write to {existing}"
                : null;

        if (reason is null)
            return (directory, null);

        var folder = Path.GetFileName(Path.TrimEndingDirectorySeparator(directory));
        return (Path.Combine(ReposDirectory(environment), folder.Length == 0 ? name : folder), reason);
    }

    public static string ForDisplay(string currentDirectory, string directory)
    {
        var relative = Path.GetRelativePath(currentDirectory, directory);
        return relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative) ? directory : relative;
    }

    private static bool IsSystemFolder(CliEnvironment environment, string directory)
    {
        if (environment.IsWindows)
            return WindowsSystemVariables.Select(environment.GetVariable).OfType<string>().Any(folder => IsUnder(directory, folder, StringComparison.OrdinalIgnoreCase));

        var comparison = environment.IsMacOS ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory)) is "/" or null || UnixSystemFolders.Any(folder => IsUnder(directory, folder, comparison));
    }

    private static bool IsUnder(string directory, string folder, StringComparison comparison)
    {
        var parent = Path.TrimEndingDirectorySeparator(folder);
        var child = Path.TrimEndingDirectorySeparator(directory);

        return child.Equals(parent, comparison) || child.StartsWith(parent + Path.DirectorySeparatorChar, comparison) || child.StartsWith(parent + Path.AltDirectorySeparatorChar, comparison);
    }

    private static string? NearestExisting(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            if (current.Exists)
                return current.FullName;
        }

        return null;
    }
}
