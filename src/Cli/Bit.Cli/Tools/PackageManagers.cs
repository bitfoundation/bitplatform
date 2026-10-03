using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tools;

public sealed record PackageManagers
{
    public string? Winget { get; init; }

    public string? Brew { get; init; }

    public string? Apt { get; init; }

    public string? Dnf { get; init; }

    public string? Pacman { get; init; }

    public string? Zypper { get; init; }

    public string? Snap { get; init; }

    public bool HasSudo { get; init; }

    public static PackageManagers Detect(CliEnvironment environment, IProcessRunner runner)
    {
        if (environment.IsWindows)
            return new PackageManagers { Winget = runner.FindExecutable("winget") };

        if (environment.IsMacOS)
        {
            return new PackageManagers
            {
                Brew = runner.FindExecutable("brew") ?? new[] { "/opt/homebrew/bin/brew", "/usr/local/bin/brew" }.FirstOrDefault(File.Exists),
                HasSudo = runner.FindExecutable("sudo") is not null
            };
        }

        return new PackageManagers
        {
            Apt = runner.FindExecutable("apt-get"),
            Dnf = runner.FindExecutable("dnf"),
            Pacman = runner.FindExecutable("pacman"),
            Zypper = runner.FindExecutable("zypper"),
            Snap = runner.FindExecutable("snap"),
            HasSudo = runner.FindExecutable("sudo") is not null || environment.IsElevated
        };
    }

    public ToolAction? WingetInstall(string toolId, string title, string packageId, bool admin = true, string? afterInstall = null)
    {
        if (Winget is null)
            return null;

        return new ToolAction
        {
            ToolId = toolId,
            Title = title,
            Elevation = admin ? Elevation.Admin : Elevation.None,
            AfterInstall = afterInstall,
            SuccessExitCodes = [0, 3010, -1978335189],
            Commands =
            [
                new ProcessSpec
                {
                    FileName = "winget",
                    Arguments = ["install", "--id", packageId, "--exact", "--source", "winget", "--silent", "--accept-package-agreements", "--accept-source-agreements", "--disable-interactivity"],
                    Timeout = TimeSpan.FromMinutes(30)
                }
            ]
        };
    }

    public ToolAction? BrewInstall(string toolId, string title, string formula, bool cask = false, string? afterInstall = null)
    {
        if (Brew is null)
            return null;

        return new ToolAction
        {
            ToolId = toolId,
            Title = title,
            Interactive = cask,
            AfterInstall = afterInstall,
            Commands = [new ProcessSpec { FileName = Brew, Arguments = cask ? ["install", "--cask", formula] : ["install", formula], Timeout = TimeSpan.FromMinutes(30) }]
        };
    }

    public ToolAction? LinuxInstall(string toolId, string title, string aptPackages, string dnfPackages, string pacmanPackages, string? afterInstall = null)
    {
        IReadOnlyList<ProcessSpec>? commands = this switch
        {
            { Apt: not null } => [Sudo("apt-get", "update"), Sudo("apt-get", ["install", "-y", .. aptPackages.Split(' ')])],
            { Dnf: not null } => [Sudo("dnf", ["install", "-y", .. dnfPackages.Split(' ')])],
            { Pacman: not null } => [Sudo("pacman", ["-S", "--noconfirm", "--needed", .. pacmanPackages.Split(' ')])],
            _ => null
        };

        return commands is null
            ? null
            : new ToolAction { ToolId = toolId, Title = title, Elevation = Elevation.Sudo, Commands = commands, AfterInstall = afterInstall };
    }

    public static ProcessSpec Sudo(string fileName, params string[] arguments)
    {
        return new ProcessSpec { FileName = fileName, Arguments = arguments, Timeout = TimeSpan.FromMinutes(30) };
    }
}
