using System.Security.Cryptography;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Keys;

public static class KeyFolder
{
    private const string PasswordCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string For(CliEnvironment environment, string scope)
    {
        var folder = Path.Combine(environment.BitDirectory, "keys", string.Concat(scope.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-')));
        Directory.CreateDirectory(folder);

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(Path.Combine(environment.BitDirectory, "keys"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return folder;
    }

    public static void WriteSecret(string file, byte[] content)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(file))!;
        Directory.CreateDirectory(directory);

        var temporary = Path.Combine(directory, $".{Path.GetFileName(file)}.{Guid.NewGuid():N}.tmp");
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        try
        {
            using (var stream = new FileStream(temporary, options))
            {
                stream.Write(content);
            }

            File.Move(temporary, file, overwrite: true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }

    public static void WriteSecret(string file, string content) => WriteSecret(file, new System.Text.UTF8Encoding(false).GetBytes(content));

    public static string NewPassword() => RandomNumberGenerator.GetString(PasswordCharacters, 32);

    public static async Task<string?> TrackedRepositoryAsync(IProcessRunner runner, string file, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(file));

        while (folder is not null && Directory.Exists(folder) is false)
        {
            folder = Path.GetDirectoryName(folder);
        }

        if (folder is null || runner.FindExecutable("git") is null)
            return null;

        var root = await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["rev-parse", "--show-toplevel"], WorkingDirectory = folder, Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);

        if (root.Succeeded is false)
            return null;

        var ignored = await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["check-ignore", "-q", "--no-index", "--", Path.GetFullPath(file)], WorkingDirectory = folder, Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);
        return ignored.ExitCode == 0 ? null : root.Output.Trim();
    }
}
