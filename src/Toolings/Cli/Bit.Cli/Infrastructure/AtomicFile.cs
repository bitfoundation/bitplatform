using System.Text;

namespace Bit.Cli.Infrastructure;

public static class AtomicFile
{
    private static readonly UTF8Encoding utf8NoBom = new(false);

    public static void WriteAllText(string path, string content, bool followLinks = false)
    {
        if (followLinks && new FileInfo(path) is { LinkTarget: not null } link)
        {
            path = link.ResolveLinkTarget(returnFinalTarget: true)!.FullName;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);

        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        UnixFileMode? mode = null;

        if ((OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && File.Exists(path))
        {
            mode = File.GetUnixFileMode(path);
            options.UnixCreateMode = mode;
        }

        try
        {
            using (var writer = new StreamWriter(temporary, utf8NoBom, options))
            {
                writer.Write(content);
            }

            if ((OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && mode is { } unixMode)
            {
                File.SetUnixFileMode(temporary, unixMode);
            }

            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }
}
