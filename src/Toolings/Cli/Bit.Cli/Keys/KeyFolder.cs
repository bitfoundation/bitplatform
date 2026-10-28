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

    public static void RestrictToOwner(string file)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public static string NewPassword() => RandomNumberGenerator.GetString(PasswordCharacters, 32);

    public static async Task<string?> TrackedRepositoryAsync(IProcessRunner runner, string file, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(file))!;

        if (Directory.Exists(folder) is false || runner.FindExecutable("git") is null)
            return null;

        var root = await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["rev-parse", "--show-toplevel"], WorkingDirectory = folder, Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);

        if (root.Succeeded is false)
            return null;

        var ignored = await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["check-ignore", "-q", "--no-index", "--", Path.GetFullPath(file)], WorkingDirectory = folder, Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);
        return ignored.ExitCode == 0 ? null : root.Output.Trim();
    }
}
