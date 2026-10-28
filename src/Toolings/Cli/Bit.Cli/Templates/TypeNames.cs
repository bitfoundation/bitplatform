using System.IO.Compression;

namespace Bit.Cli.Templates;

public static class TypeNames
{
    private static readonly HashSet<string> skippedFolders = new(StringComparer.OrdinalIgnoreCase) { ".git", "bin", "obj", "node_modules", ".vs" };

    private static readonly Lazy<HashSet<string>> template = new(LoadEmbedded);

    public static IReadOnlySet<string> Template => template.Value;

    public static IReadOnlySet<string> FromPackage(string packagePath)
    {
        if (Directory.Exists(packagePath))
            return FromFolder(packagePath);

        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            return Collect(archive.Entries.Select(entry => entry.Name));
        }
        catch (Exception exp) when (exp is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return Template;
        }
    }

    public static IReadOnlySet<string> FromFolder(string folder) => Collect(Files(new DirectoryInfo(folder)).Select(file => file.Name));

    public static string? FindFile(string folder, string typeName) => Files(new DirectoryInfo(folder)).FirstOrDefault(file => Of(file.Name) == typeName)?.FullName;

    public static string? Of(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) || extension.Equals(".razor", StringComparison.OrdinalIgnoreCase)
            ? fileName[..fileName.IndexOf('.')]
            : null;
    }

    private static HashSet<string> Collect(IEnumerable<string> fileNames)
    {
        return [.. fileNames.Select(Of).OfType<string>().Where(name => name.Length > 0)];
    }

    private static IEnumerable<FileInfo> Files(DirectoryInfo folder)
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
            if (entry is FileInfo file)
            {
                yield return file;
            }
            else if (entry is DirectoryInfo directory && skippedFolders.Contains(directory.Name) is false && directory.Attributes.HasFlag(FileAttributes.ReparsePoint) is false)
            {
                foreach (var nested in Files(directory))
                {
                    yield return nested;
                }
            }
        }
    }

    private static HashSet<string> LoadEmbedded()
    {
        using var stream = typeof(TypeNames).Assembly.GetManifestResourceStream("Bit.Cli.template-types.txt")
            ?? throw new InvalidOperationException("The bit Boilerplate's type names aren't embedded in this build.");
        using var reader = new StreamReader(stream);
        return Collect(reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
