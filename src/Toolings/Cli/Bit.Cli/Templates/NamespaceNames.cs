using System.IO.Compression;

namespace Bit.Cli.Templates;

public static class NamespaceNames
{
    private const string TemplateRootName = "Boilerplate";

    private static readonly HashSet<string> skippedFolders = new(StringComparer.OrdinalIgnoreCase) { ".git", "bin", "obj", "node_modules", ".vs" };

    private static readonly Lazy<HashSet<string>> template = new(LoadEmbedded);

    public static IReadOnlySet<string> Template => template.Value;

    public static IReadOnlySet<string> FromPackage(string packagePath)
    {
        if (Directory.Exists(packagePath))
            return FromFolder(packagePath, TemplateRootName);

        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            return Collect(archive.Entries.Where(entry => TypeNames.Of(entry.Name) is not null).Select(entry => SourceFolder(entry.FullName)).OfType<string>(), TemplateRootName);
        }
        catch (Exception exp) when (exp is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return Template;
        }
    }

    public static IReadOnlySet<string> FromFolder(string folder, string rootName)
    {
        return Collect(CodeFolders(new DirectoryInfo(folder), "").Select(SourceFolder).OfType<string>(), rootName);
    }

    internal static HashSet<string> Collect(IEnumerable<string> folders, string rootName)
    {
        var rootSegments = rootName.Split('.');

        return [.. folders.SelectMany(folder => folder.Split(['/', '\\', '.'], StringSplitOptions.RemoveEmptyEntries))
                          .Where(segment => (char.IsLetter(segment[0]) || segment[0] is '_') && segment.All(c => char.IsLetterOrDigit(c) || c is '_'))
                          .Except(rootSegments)];
    }

    private static string? SourceFolder(string path)
    {
        var normalized = $"/{path.Replace('\\', '/')}";
        var start = normalized.IndexOf("/src/", StringComparison.Ordinal);

        if (start < 0)
            return null;

        var relative = normalized[(start + "/src/".Length)..];
        var lastSlash = relative.LastIndexOf('/');

        return lastSlash < 0 ? null : relative[..lastSlash];
    }

    private static IEnumerable<string> CodeFolders(DirectoryInfo folder, string relativePath)
    {
        FileSystemInfo[] entries;

        try
        {
            entries = folder.GetFileSystemInfos();
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var entry in entries)
        {
            if (entry is FileInfo file && TypeNames.Of(file.Name) is not null)
            {
                yield return $"{relativePath}{file.Name}";
            }
            else if (entry is DirectoryInfo directory && skippedFolders.Contains(directory.Name) is false && directory.Attributes.HasFlag(FileAttributes.ReparsePoint) is false)
            {
                foreach (var nested in CodeFolders(directory, $"{relativePath}{directory.Name}/"))
                {
                    yield return nested;
                }
            }
        }
    }

    private static HashSet<string> LoadEmbedded()
    {
        using var stream = typeof(NamespaceNames).Assembly.GetManifestResourceStream("Bit.Cli.template-namespaces.txt")
            ?? throw new InvalidOperationException("The bit Boilerplate's namespaces aren't embedded in this build.");
        using var reader = new StreamReader(stream);
        return Collect(reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), TemplateRootName);
    }
}
