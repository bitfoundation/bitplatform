namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Finds the .razor file of a component class under the src folder, and turns its path into one relative to that folder and back.</summary>
public static class WorkbenchRazorFiles
{
    private static readonly string[] ignoredFolders = ["bin", "obj", "node_modules", ".git", ".vs"];
    private static readonly Lock scanLock = new();
    private static Dictionary<string, string[]>? filesByName;
    private static DateTimeOffset scannedAt;

    /// <summary>The .razor file of a component class; of several with its name, the one whose folders end like its namespace.</summary>
    public static string? Find(Type type)
    {
        if (WorkbenchFolders.Source is not { } source)
            return null;

        var name = type.Name.Split('`')[0];
        var segments = (type.Namespace ?? string.Empty).Split('.');

        return FilesNamed(source, name)
            .OrderByDescending(path => MatchingTail(Path.GetDirectoryName(path)!.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), segments))
            .FirstOrDefault();
    }

    public static string Relative(string path) => Path.GetRelativePath(WorkbenchFolders.Source!, path).Replace('\\', '/');

    /// <summary>The full path of a .razor file given relative to the src folder; null when it isn't there or lies outside the folder.</summary>
    public static string? FullPath(string relative)
    {
        if (WorkbenchFolders.Source is not { } source)
            return null;

        var root = Path.GetFullPath(source) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative));

        return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) && File.Exists(path) ? path : null;
    }

    /// <summary>The files with the name, looking again when none is known and the last look is over ten seconds old.</summary>
    private static string[] FilesNamed(string source, string name)
    {
        lock (scanLock)
        {
            if (filesByName is null || (filesByName.ContainsKey(name) is false && DateTimeOffset.UtcNow - scannedAt > TimeSpan.FromSeconds(10)))
            {
                filesByName = Enumerate(source).GroupBy(Path.GetFileNameWithoutExtension, StringComparer.Ordinal).ToDictionary(group => group.Key!, group => group.ToArray(), StringComparer.Ordinal);
                scannedAt = DateTimeOffset.UtcNow;
            }

            return filesByName.GetValueOrDefault(name) ?? [];
        }
    }

    private static IEnumerable<string> Enumerate(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.razor"))
        {
            yield return file;
        }

        foreach (var child in Directory.EnumerateDirectories(folder))
        {
            if (ignoredFolders.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
                continue;

            foreach (var file in Enumerate(child))
            {
                yield return file;
            }
        }
    }

    /// <summary>How many of the last folders match the last namespace segments, in order.</summary>
    private static int MatchingTail(string[] folders, string[] segments)
    {
        var count = 0;

        while (count < folders.Length && count < segments.Length && string.Equals(folders[^(count + 1)], segments[^(count + 1)], StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }

        return count;
    }
}
