using System.Text;

namespace Bit.Cli.Projects;

public sealed class ProjectRenamer
{
    private const int BinaryProbeLength = 8192;

    private static readonly HashSet<string> skippedFolders = new(StringComparer.OrdinalIgnoreCase) { ".git", "bin", "obj", "node_modules", ".vs" };

    private static readonly HashSet<string> buildFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs" };

    private static readonly HashSet<string> binaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".bmp", ".tif", ".tiff", ".avif", ".icns",
        ".woff", ".woff2", ".ttf", ".otf", ".eot", ".pdf", ".zip", ".7z", ".tar", ".nupkg", ".snupkg",
        ".db", ".sqlite", ".sqlite3", ".dll", ".exe", ".pdb", ".so", ".dylib", ".snk", ".pfx", ".p12", ".jks", ".keystore",
        ".mp3", ".mp4", ".wav", ".wasm", ".br", ".gz"
    };

    private static readonly Encoding utf8 = new UTF8Encoding(false, true);

    private static readonly Encoding[] encodingsWithBom =
    [
        new UTF8Encoding(true, true),
        new UTF32Encoding(false, true, true),
        new UTF32Encoding(true, true, true),
        new UnicodeEncoding(false, true, true),
        new UnicodeEncoding(true, true, true)
    ];

    private readonly (string Old, string New)[] replacements;

    public ProjectRenamer(string oldName, string newName)
    {
        ArgumentException.ThrowIfNullOrEmpty(oldName);

        var pairs = new List<(string Old, string New)> { (oldName, newName) };
        var oldLower = oldName.ToLowerInvariant();

        if (string.Equals(oldLower, oldName, StringComparison.Ordinal) is false)
        {
            pairs.Add((oldLower, newName.ToLowerInvariant()));
        }

        if (oldName.Contains('.'))
        {
            pairs.Add((oldName.Replace('.', '_'), newName.Replace('.', '_')));
        }

        replacements = [.. pairs];
    }

    public string Replace(string text)
    {
        StringBuilder? builder = null;
        var next = new int[replacements.Length];
        var copied = 0;

        Array.Fill(next, -2);

        while (true)
        {
            var best = -1;

            for (var i = 0; i < replacements.Length; i++)
            {
                if (next[i] is not -1 && next[i] < copied)
                {
                    next[i] = Find(text, replacements[i].Old, copied);
                }

                if (next[i] >= 0 && (best < 0 || next[i] < next[best]))
                {
                    best = i;
                }
            }

            if (best < 0)
                break;

            builder ??= new StringBuilder(text.Length + 64);
            builder.Append(text, copied, next[best] - copied).Append(replacements[best].New);
            copied = next[best] + replacements[best].Old.Length;
        }

        return builder is null ? text : builder.Append(text, copied, text.Length - copied).ToString();
    }

    public static (int Deleted, IReadOnlyList<PathFailure> Failures) DeleteBuildFolders(string root)
    {
        var deleted = 0;
        var failures = new List<PathFailure>();

        foreach (var folder in BuildFolders(new DirectoryInfo(root)).ToList())
        {
            try
            {
                DeleteFolder(folder);
                deleted++;
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                failures.Add(new PathFailure(folder.FullName, exp.Message));
            }
        }

        return (deleted, failures);
    }

    public (int Changed, IReadOnlyList<PathFailure> Failures) ReplaceInFiles(string root)
    {
        var changed = 0;
        var failures = new List<PathFailure>();

        foreach (var file in Entries(new DirectoryInfo(root)).OfType<FileInfo>().ToList())
        {
            if (file.Attributes.HasFlag(FileAttributes.ReparsePoint) || binaryExtensions.Contains(file.Extension))
                continue;

            try
            {
                if (ReplaceInFile(file.FullName))
                {
                    changed++;
                }
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                failures.Add(new PathFailure(file.FullName, exp.Message));
            }
        }

        return (changed, failures);
    }

    public (int Files, int Folders, IReadOnlyList<PathFailure> Failures) RenamePaths(string root)
    {
        var files = 0;
        var folders = 0;
        var failures = new List<PathFailure>();
        var entries = Entries(new DirectoryInfo(root)).OrderByDescending(e => e.FullName.Count(c => c == Path.DirectorySeparatorChar)).ToList();

        foreach (var entry in entries)
        {
            var newName = Replace(entry.Name);

            if (string.Equals(newName, entry.Name, StringComparison.Ordinal))
                continue;

            var source = entry.FullName;

            try
            {
                Move(entry, Path.Combine(Path.GetDirectoryName(source)!, newName));

                if (entry is DirectoryInfo)
                {
                    folders++;
                }
                else
                {
                    files++;
                }
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                failures.Add(new PathFailure(source, exp.Message));
            }
        }

        return (files, folders, failures);
    }

    private bool ReplaceInFile(string path)
    {
        var bytes = File.ReadAllBytes(path);

        if (DetectEncoding(bytes) is not { } detected)
            return false;

        var (encoding, preambleLength) = detected;
        string text;

        try
        {
            text = encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        var replaced = Replace(text);

        if (string.Equals(replaced, text, StringComparison.Ordinal))
            return false;

        var body = encoding.GetBytes(replaced);
        var output = new byte[preambleLength + body.Length];
        bytes.AsSpan(0, preambleLength).CopyTo(output);
        body.CopyTo(output, preambleLength);

        var temp = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllBytes(temp, output);

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(temp, File.GetUnixFileMode(path));
            }

            File.Replace(temp, path, destinationBackupFileName: null);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }

        return true;
    }

    private static (Encoding Encoding, int PreambleLength)? DetectEncoding(byte[] bytes)
    {
        foreach (var encoding in encodingsWithBom)
        {
            var preamble = encoding.Preamble;

            if (bytes.AsSpan().StartsWith(preamble))
                return (encoding, preamble.Length);
        }

        return bytes.AsSpan(0, Math.Min(bytes.Length, BinaryProbeLength)).Contains((byte)0) ? null : (utf8, 0);
    }

    private static int Find(string text, string value, int start)
    {
        var index = text.IndexOf(value, start, StringComparison.Ordinal);

        while (index >= 0 && IsInsideWord(text, index, value.Length))
        {
            index = text.IndexOf(value, index + 1, StringComparison.Ordinal);
        }

        return index;
    }

    private static bool IsInsideWord(string text, int index, int length)
    {
        var end = index + length;

        return (index > 0 && IsWordCharacter(text[index - 1])) || (end < text.Length && char.IsLower(text[end]));
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c is '_';

    private static IEnumerable<FileSystemInfo> Entries(DirectoryInfo folder)
    {
        foreach (var entry in folder.EnumerateFileSystemInfos())
        {
            if (skippedFolders.Contains(entry.Name))
                continue;

            yield return entry;

            if (entry is DirectoryInfo directory && directory.Attributes.HasFlag(FileAttributes.ReparsePoint) is false)
            {
                foreach (var nested in Entries(directory))
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<DirectoryInfo> BuildFolders(DirectoryInfo folder)
    {
        foreach (var directory in folder.EnumerateDirectories())
        {
            if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint) || directory.Name.Equals(".git", StringComparison.OrdinalIgnoreCase) || directory.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                continue;

            if (buildFolders.Contains(directory.Name))
            {
                yield return directory;
                continue;
            }

            foreach (var nested in BuildFolders(directory))
            {
                yield return nested;
            }
        }
    }

    private static void DeleteFolder(DirectoryInfo folder)
    {
        try
        {
            folder.Delete(recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
            foreach (var file in folder.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            folder.Delete(recursive: true);
        }
    }

    private static void Move(FileSystemInfo entry, string target)
    {
        var caseOnly = string.Equals(entry.FullName, target, StringComparison.OrdinalIgnoreCase);

        if (caseOnly is false && Path.Exists(target))
            throw new IOException($"{target} already exists.");

        if (caseOnly)
        {
            MoveTo(entry, $"{target}.{Guid.NewGuid():N}");
        }

        MoveTo(entry, target);
    }

    private static void MoveTo(FileSystemInfo entry, string target)
    {
        if (entry is DirectoryInfo directory)
        {
            directory.MoveTo(target);
        }
        else
        {
            ((FileInfo)entry).MoveTo(target);
        }
    }
}
