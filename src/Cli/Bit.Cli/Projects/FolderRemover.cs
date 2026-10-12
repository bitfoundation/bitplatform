namespace Bit.Cli.Projects;

public static class FolderRemover
{
    public static (IReadOnlyList<PathFailure> Failures, bool OnlyEmptyFoldersLeft) Delete(string folder)
    {
        var failures = new List<PathFailure>();

        DeleteTree(new DirectoryInfo(folder), failures);

        if (Directory.Exists(folder) is false)
            return ([], false);

        return HasFiles(folder) ? (failures, false) : ([], true);
    }

    private static void DeleteTree(DirectoryInfo folder, List<PathFailure> failures)
    {
        var before = failures.Count;
        List<FileSystemInfo> entries;

        try
        {
            ClearReadOnly(folder);
            entries = [.. folder.EnumerateFileSystemInfos()];
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            failures.Add(new PathFailure(folder.FullName, exp.Message));
            return;
        }

        foreach (var entry in entries)
        {
            if (entry is DirectoryInfo directory && directory.Attributes.HasFlag(FileAttributes.ReparsePoint) is false)
            {
                DeleteTree(directory, failures);
            }
            else
            {
                TryDelete(entry, failures);
            }
        }

        if (failures.Count == before)
        {
            TryDelete(folder, failures);
        }
    }

    private static void TryDelete(FileSystemInfo entry, List<PathFailure> failures)
    {
        try
        {
            ClearReadOnly(entry);
            entry.Delete();
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            failures.Add(new PathFailure(entry.FullName, exp.Message));
        }
    }

    private static void ClearReadOnly(FileSystemInfo entry)
    {
        var attributes = entry.Attributes;

        if (attributes.HasFlag(FileAttributes.ReadOnly) && attributes.HasFlag(FileAttributes.ReparsePoint) is false)
        {
            entry.Attributes = attributes & ~FileAttributes.ReadOnly;
        }
    }

    private static bool HasFiles(string folder)
    {
        try
        {
            return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Any();
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }
}
