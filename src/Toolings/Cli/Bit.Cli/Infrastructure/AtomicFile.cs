using System.Text;

namespace Bit.Cli.Infrastructure;

public static class AtomicFile
{
    private static readonly UTF8Encoding utf8NoBom = new(false);

    public static void WriteAllText(string path, string content)
    {
        if (new FileInfo(path) is { LinkTarget: not null } link)
        {
            path = link.ResolveLinkTarget(returnFinalTarget: true)!.FullName;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);

        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporary, content, utf8NoBom);

        try
        {
            if ((OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && File.Exists(path))
            {
                File.SetUnixFileMode(temporary, File.GetUnixFileMode(path));
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
