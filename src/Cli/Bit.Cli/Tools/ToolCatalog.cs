using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Microsoft.Win32;

namespace Bit.Cli.Tools;

public static partial class ToolCatalog
{
    public static IReadOnlyList<Tool> All { get; } =
    [
        new DotnetSdkTool(),
        new GitTool(),
        new GitHubCliTool(),
        new NodeTool(),
        new HomebrewTool(),
        new WslTool(),
        new DockerTool(),
        new AspireCliTool(),
        new DevCertificateTool(),
        new LongPathsTool(),
        new HypervisorPlatformTool(),
        new DeveloperModeTool(),
        new PythonTool(),
        new XcodeTool(),
        new VsCodeTool(),
        new VisualStudioTool()
    ];

    public static Tool? Find(string id) => All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    public static async Task<IReadOnlyList<ToolCheck>> CheckAsync(ToolContext context, CancellationToken cancellationToken)
    {
        var applicable = All.Where(t => t.AppliesTo(context) && (context.Environment.IsCI is false || t.AppliesInCi(context))).ToArray();
        var statuses = await Task.WhenAll(applicable.Select(async tool =>
        {
            try
            {
                return await tool.DetectAsync(context, cancellationToken);
            }
            catch (Exception exp) when (exp is not OperationCanceledException)
            {
                return ToolStatus.Missing(exp.Message);
            }
        }));

        var checks = applicable.Select((tool, i) => new ToolCheck(tool, statuses[i], tool.IsNeeded(context), tool.Why(context), statuses[i].IsSatisfied ? null : tool.PlanInstall(context, statuses[i]), tool.ManualInstructions(context))).ToList();

        var dockerMissing = checks.Any(c => c.Tool is DockerTool && c.Status.State is ToolState.Missing);
        checks.RemoveAll(c => c.Tool is WslTool && dockerMissing is false);

        var brewNeeded = checks.Any(c => c.Status.IsSatisfied is false && c.Tool is not HomebrewTool && c.Action is null && context.Environment.IsMacOS && context.PackageManagers.Brew is null && c.Tool is NodeTool or GitTool or DockerTool or VsCodeTool);
        checks.RemoveAll(c => c.Tool is HomebrewTool && (c.Status.IsSatisfied || brewNeeded is false));

        return checks;
    }

    internal static Version? ParseVersion(string text)
    {
        var match = VersionRegex().Match(text);
        return match.Success && Version.TryParse(match.Value, out var version) ? version : null;
    }

    [GeneratedRegex(@"\d+\.\d+(\.\d+)?")]
    private static partial Regex VersionRegex();

    private sealed class DotnetSdkTool : Tool
    {
        public override string Id => "dotnet-sdk";

        public override string Name => ".NET SDK";

        public override bool AppliesTo(ToolContext context) => context.Needs.MinimumSdk is not null;

        public override string Why(ToolContext context) => $"the project's global.json asks for SDK {context.Needs.MinimumSdk} or a later patch";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("dotnet", ["--list-sdks"], cancellationToken);
            var minimum = context.Needs.MinimumSdk!;
            var versions = result.OutputLines.Select(l => ParseVersion(l.Split(' ')[0])).OfType<Version>().ToArray();
            var match = versions.Where(v => v.Major == minimum.Major && v.Minor == minimum.Minor && v >= minimum).Max();

            return match is not null ? ToolStatus.Installed(match.ToString()) : ToolStatus.Missing($"found {(versions.Length == 0 ? "none" : string.Join(", ", versions.Select(v => v.ToString())))}");
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status)
        {
            var minimum = context.Needs.MinimumSdk!;
            return context.Environment.IsWindows ? context.PackageManagers.WingetInstall(Id, $"Install .NET SDK {minimum.Major}.{minimum.Minor}", $"Microsoft.DotNet.SDK.{minimum.Major}") : null;
        }

        public override string? ManualInstructions(ToolContext context) => $"https://dotnet.microsoft.com/download/dotnet/{context.Needs.MinimumSdk?.Major}.{context.Needs.MinimumSdk?.Minor}";
    }

    private sealed class GitTool : Tool
    {
        public override string Id => "git";

        public override string Name => "Git";

        public override string Why(ToolContext context) => "version control, and the develop and main branches of the new project";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("git", ["--version"], cancellationToken);
            return result.Succeeded ? ToolStatus.Installed(ParseVersion(result.Output)?.ToString()) : ToolStatus.Missing();
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => context.Environment.Os switch
        {
            HostOs.Windows => context.PackageManagers.WingetInstall(Id, "Install Git", "Git.Git"),
            HostOs.MacOS => context.PackageManagers.BrewInstall(Id, "Install Git", "git"),
            _ => context.PackageManagers.LinuxInstall(Id, "Install Git", "git", "git", "git")
        };

        public override string? ManualInstructions(ToolContext context) => "https://git-scm.com/downloads";
    }

    private sealed class GitHubCliTool : Tool
    {
        public override string Id => "gh";

        public override string Name => "GitHub CLI";

        public override bool AppliesTo(ToolContext context) => context.Needs.GitHubRepo;

        public override string Why(ToolContext context) => "creates the project's private GitHub repository and pushes develop and main to it";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("gh", ["--version"], cancellationToken);
            return result.Succeeded ? ToolStatus.Installed(ParseVersion(result.Output)?.ToString()) : ToolStatus.Missing();
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => context.Environment.Os switch
        {
            HostOs.Windows => context.PackageManagers.WingetInstall(Id, "Install the GitHub CLI", "GitHub.cli"),
            HostOs.MacOS => context.PackageManagers.BrewInstall(Id, "Install the GitHub CLI", "gh"),
            _ => context.PackageManagers.LinuxInstall(Id, "Install the GitHub CLI", "gh", "gh", "github-cli")
        };

        public override string? ManualInstructions(ToolContext context) => "https://cli.github.com";
    }

    private sealed class NodeTool : Tool
    {
        public const int MinimumMajor = 20;

        public override string Id => "node";

        public override string Name => "Node.js";

        public override string Why(ToolContext context) => "the build runs npm, TypeScript and Sass to produce the app's CSS and JS";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("node", ["--version"], cancellationToken);

            if (result.Succeeded is false)
                return ToolStatus.Missing();

            var version = ParseVersion(result.Output);
            var minimum = context.Needs.NodeMajor ?? MinimumMajor;

            return version is null || version.Major < minimum
                ? new ToolStatus(ToolState.Outdated, version?.ToString(), $"{minimum} or later is needed")
                : ToolStatus.Installed(version.ToString());
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status)
        {
            var title = status.State is ToolState.Outdated ? "Update Node.js to the LTS version" : "Install Node.js LTS";

            if (context.Environment.IsWindows)
                return context.PackageManagers.WingetInstall(Id, title, "OpenJS.NodeJS.LTS");

            if (context.Environment.IsMacOS)
                return context.PackageManagers.BrewInstall(Id, title, "node");

            if (context.PackageManagers.Apt is not null)
            {
                return new ToolAction
                {
                    ToolId = Id,
                    Title = title,
                    Elevation = Elevation.Sudo,
                    Commands =
                    [
                        PackageManagers.Sudo("bash", "-c", "curl -fsSL https://deb.nodesource.com/setup_lts.x | bash -"),
                        PackageManagers.Sudo("apt-get", "install", "-y", "nodejs")
                    ]
                };
            }

            return context.PackageManagers.LinuxInstall(Id, title, "nodejs npm", "nodejs npm", "nodejs npm");
        }

        public override string? ManualInstructions(ToolContext context) => "https://nodejs.org/en/download";
    }

    private sealed class HomebrewTool : Tool
    {
        public override string Id => "homebrew";

        public override string Name => "Homebrew";

        public override bool AppliesTo(ToolContext context) => context.Environment.IsMacOS;

        public override string Why(ToolContext context) => "installs the other tools on macOS";

        public override bool IsNeeded(ToolContext context) => true;

        public override Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(context.PackageManagers.Brew is null ? ToolStatus.Missing() : ToolStatus.Installed());
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => new()
        {
            ToolId = Id,
            Title = "Install Homebrew",
            Interactive = true,
            Commands = [new ProcessSpec { FileName = "/bin/bash", Arguments = ["-c", "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"], Interactive = true }]
        };

        public override string? ManualInstructions(ToolContext context) => "https://brew.sh";
    }

    private sealed class WslTool : Tool
    {
        public override string Id => "wsl";

        public override string Name => "WSL";

        public override bool AppliesInCi(ToolContext context) => false;

        public override bool AppliesTo(ToolContext context) => context.Environment.IsWindows && context.Needs.Aspire;

        public override string Why(ToolContext context) => "Docker Desktop runs its containers in WSL 2";

        public override bool IsNeeded(ToolContext context) => context.Needs.Aspire;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("wsl.exe", ["--status"], cancellationToken, environment: new Dictionary<string, string?> { ["WSL_UTF8"] = "1" });
            return result.Succeeded ? ToolStatus.Installed() : ToolStatus.Missing();
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => new()
        {
            ToolId = Id,
            Title = "Install WSL",
            Elevation = Elevation.Admin,
            AfterInstall = "Restart Windows to finish installing WSL.",
            SuccessExitCodes = [0, 3010],
            Commands = [new ProcessSpec { FileName = "wsl.exe", Arguments = ["--install", "--no-distribution"], Timeout = TimeSpan.FromMinutes(30) }]
        };

        public override string? ManualInstructions(ToolContext context) => "https://learn.microsoft.com/windows/wsl/install";
    }

    private sealed class DockerTool : Tool
    {
        public override string Id => "docker";

        public override string Name => "Docker";

        public override bool AppliesInCi(ToolContext context) => false;

        public override string Why(ToolContext context) => context.Needs switch
        {
            { Aspire: false } => "runs containers; this project doesn't need it without Aspire",
            { Containers.Count: > 0 } needs => $"Aspire runs {Join(needs.Containers)} in containers",
            _ => "Aspire runs the database, Keycloak, Mailpit and other resources in containers"
        };

        private static string Join(IReadOnlyList<string> items) => items.Count == 1 ? items[0] : $"{string.Join(", ", items.SkipLast(1))} and {items[^1]}";

        public override bool IsNeeded(ToolContext context) => context.Needs.Aspire;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var server = await context.RunAsync("docker", ["version", "--format", "{{.Server.Version}}"], cancellationToken, TimeSpan.FromSeconds(20));

            if (server.Succeeded && ParseVersion(server.Output) is { } version)
                return ToolStatus.Installed(version.ToString());

            var client = await context.RunAsync("docker", ["--version"], cancellationToken);
            return client.Succeeded ? new ToolStatus(ToolState.NotRunning, ParseVersion(client.Output)?.ToString(), "installed, but not running") : ToolStatus.Missing();
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status)
        {
            if (status.State is ToolState.NotRunning)
            {
                return context.Environment.Os switch
                {
                    HostOs.Windows when FindDockerDesktop(context) is { } desktop => new ToolAction
                    {
                        ToolId = Id,
                        Title = "Start Docker Desktop",
                        Commands = [new ProcessSpec { FileName = "cmd.exe", Arguments = ["/c", "start", "", desktop] }],
                        AfterInstall = "Docker Desktop is starting; accept its terms if it asks."
                    },
                    HostOs.MacOS => new ToolAction { ToolId = Id, Title = "Start Docker Desktop", Commands = [new ProcessSpec { FileName = "open", Arguments = ["-a", "Docker"] }] },
                    HostOs.Linux => new ToolAction { ToolId = Id, Title = "Start Docker", Elevation = Elevation.Sudo, Commands = [PackageManagers.Sudo("systemctl", "start", "docker")] },
                    _ => null
                };
            }

            return context.Environment.Os switch
            {
                HostOs.Windows => context.PackageManagers.WingetInstall(Id, "Install Docker Desktop", "Docker.DockerDesktop", afterInstall: "Start Docker Desktop once and accept its terms; Windows may need a restart first."),
                HostOs.MacOS => context.PackageManagers.BrewInstall(Id, "Install Docker Desktop", "docker", cask: true, afterInstall: "Open Docker Desktop once and accept its terms."),
                _ when context.PackageManagers.Apt is not null || context.PackageManagers.Dnf is not null => new ToolAction
                {
                    ToolId = Id,
                    Title = "Install Docker Engine",
                    Elevation = Elevation.Sudo,
                    AfterInstall = "Sign out and back in so your user can run docker without sudo.",
                    Commands =
                    [
                        PackageManagers.Sudo("sh", "-c", "curl -fsSL https://get.docker.com | sh"),
                        PackageManagers.Sudo("usermod", "-aG", "docker", context.Environment.GetVariable("USER") ?? Environment.UserName),
                        PackageManagers.Sudo("systemctl", "enable", "--now", "docker")
                    ]
                },
                _ => context.PackageManagers.LinuxInstall(Id, "Install Docker", "docker.io", "docker", "docker")
            };
        }

        public override string? ManualInstructions(ToolContext context) => context.Environment.IsLinux ? "https://docs.docker.com/engine/install/" : "https://www.docker.com/products/docker-desktop/";

        private static string? FindDockerDesktop(ToolContext context)
        {
            var path = Path.Combine(context.Environment.GetVariable("ProgramFiles") ?? @"C:\Program Files", "Docker", "Docker", "Docker Desktop.exe");
            return File.Exists(path) ? path : null;
        }
    }

    private sealed class AspireCliTool : Tool
    {
        public override string Id => "aspire";

        public override string Name => "Aspire CLI";

        public override bool AppliesInCi(ToolContext context) => false;

        public override bool AppliesTo(ToolContext context) => context.Needs.Aspire;

        public override string Why(ToolContext context) => "runs the project with aspire start, and backs the aspire MCP server";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("aspire", ["--version"], cancellationToken);

            if (result.Succeeded is false)
                return ToolStatus.Missing();

            var version = ParseVersion(result.Output);

            return context.Needs.AspireVersion is { } wanted && (version is null || Normalize(version) < Normalize(wanted))
                ? new ToolStatus(ToolState.Outdated, version?.ToString(), $"the AppHost uses Aspire {wanted}")
                : ToolStatus.Installed(version?.ToString());
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status)
        {
            var wanted = context.Needs.AspireVersion?.ToString();
            var outdated = status.State is ToolState.Outdated;
            string[] version = wanted is null ? [] : ["--version", wanted];

            return new ToolAction
            {
                ToolId = Id,
                Title = outdated ? $"Update the Aspire CLI to {wanted}" : wanted is null ? "Install the Aspire CLI" : $"Install the Aspire CLI {wanted}",
                Commands = [new ProcessSpec { FileName = "dotnet", Arguments = ["tool", outdated ? "update" : "install", "--global", "Aspire.Cli", .. version], Timeout = TimeSpan.FromMinutes(10) }]
            };
        }

        private static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));
    }

    private sealed class DevCertificateTool : Tool
    {
        public override string Id => "dev-cert";

        public override string Name => "HTTPS development certificate";

        public override bool AppliesInCi(ToolContext context) => context.Environment.IsLinux;

        public override string Why(ToolContext context) => "lets browsers trust https://localhost, where the app and the Aspire dashboard run";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("dotnet", ["dev-certs", "https", "--check", "--trust"], cancellationToken);
            return result.Succeeded ? ToolStatus.Installed() : ToolStatus.Missing("not trusted");
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => new()
        {
            ToolId = Id,
            Title = "Trust the HTTPS development certificate",
            Interactive = true,
            Optional = true,
            Partial = context.Environment.IsLinux
                ? new PartialSuccess(4, "for some clients", "For .NET clients, add ~/.aspnet/dev-certs/trust to SSL_CERT_DIR")
                : null,
            AfterInstall = null,
            Commands = [new ProcessSpec { FileName = "dotnet", Arguments = ["dev-certs", "https", "--trust"], Interactive = true, Timeout = TimeSpan.FromMinutes(5) }]
        };
    }

    private sealed class LongPathsTool : Tool
    {
        private const string Key = @"SYSTEM\CurrentControlSet\Control\FileSystem";

        public override string Id => "long-paths";

        public override string Name => "Windows long paths";

        public override bool AppliesTo(ToolContext context) => context.Environment.IsWindows;

        public override string Why(ToolContext context) => "MAUI builds and npm packages can exceed the 260 character path limit";

        public override bool IsNeeded(ToolContext context) => true;

        public override Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(OperatingSystem.IsWindows() && IsEnabled() ? ToolStatus.Installed() : ToolStatus.Missing());
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static bool IsEnabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(Key);
            return key?.GetValue("LongPathsEnabled") is int value && value == 1;
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => new()
        {
            ToolId = Id,
            Title = "Enable Windows long paths",
            Elevation = Elevation.Admin,
            Commands = [new ProcessSpec { FileName = "reg.exe", Arguments = ["add", $@"HKLM\{Key}", "/v", "LongPathsEnabled", "/t", "REG_DWORD", "/d", "1", "/f"] }]
        };
    }

    private sealed class HypervisorPlatformTool : Tool
    {
        public override string Id => "hypervisor-platform";

        public override string Name => "Windows Hypervisor Platform";

        public override bool AppliesInCi(ToolContext context) => false;

        public override bool AppliesTo(ToolContext context) => context.Environment.IsWindows && context.Needs.Platforms.Contains(Platform.Android);

        public override string Why(ToolContext context) => "lets the Android emulator run with hardware acceleration next to WSL and Docker";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("powershell.exe", ["-NoProfile", "-NonInteractive", "-Command", "(Get-CimInstance Win32_OptionalFeature -Filter \"Name='HypervisorPlatform'\").InstallState"], cancellationToken);
            return result.Succeeded && result.Output.Trim() == "1" ? ToolStatus.Installed() : ToolStatus.Missing();
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => new()
        {
            ToolId = Id,
            Title = "Turn on the Windows Hypervisor Platform",
            Elevation = Elevation.Admin,
            AfterInstall = "Restart Windows to finish turning on the Windows Hypervisor Platform.",
            SuccessExitCodes = [0, 3010],
            Commands = [new ProcessSpec { FileName = "dism.exe", Arguments = ["/online", "/enable-feature", "/featurename:HypervisorPlatform", "/all", "/norestart"], Timeout = TimeSpan.FromMinutes(30) }]
        };

        public override string? ManualInstructions(ToolContext context) => "https://learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration";
    }

    private sealed class DeveloperModeTool : Tool
    {
        private const string Key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";

        public override string Id => "developer-mode";

        public override string Name => "Windows developer mode";

        public override bool AppliesInCi(ToolContext context) => false;

        public override bool AppliesTo(ToolContext context) => context.Environment.IsWindows && context.Needs.NeedsMaui;

        public override string Why(ToolContext context) => "runs the Windows version of the MAUI app from VS and VS Code";

        public override Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(OperatingSystem.IsWindows() && IsEnabled() ? ToolStatus.Installed() : ToolStatus.Missing());
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static bool IsEnabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(Key);
            return key?.GetValue("AllowDevelopmentWithoutDevLicense") is int value && value == 1;
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => new()
        {
            ToolId = Id,
            Title = "Turn on Windows developer mode",
            Elevation = Elevation.Admin,
            Commands = [new ProcessSpec { FileName = "reg.exe", Arguments = ["add", $@"HKLM\{Key}", "/v", "AllowDevelopmentWithoutDevLicense", "/t", "REG_DWORD", "/d", "1", "/f"] }]
        };
    }

    private sealed class PythonTool : Tool
    {
        public override string Id => "python";

        public override string Name => "Python 3";

        public override bool AppliesTo(ToolContext context) => context.Environment.IsLinux && context.Needs.NativeWebAssembly;

        public override string Why(ToolContext context) => "the native WebAssembly build of the offline database runs Emscripten, which needs it";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("python3", ["--version"], cancellationToken);
            return result.Succeeded ? ToolStatus.Installed(ParseVersion(result.Output)?.ToString()) : ToolStatus.Missing();
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => context.PackageManagers.LinuxInstall(Id, "Install Python 3", "python3", "python3", "python");

        public override string? ManualInstructions(ToolContext context) => "https://www.python.org/downloads/";
    }

    private sealed class XcodeTool : Tool
    {
        public override string Id => "xcode";

        public override string Name => "Xcode";

        public override bool AppliesTo(ToolContext context) => context.Environment.IsMacOS && context.Needs.NeedsMaui;

        public override string Why(ToolContext context) => "builds the iOS and macOS apps";

        public override bool IsNeeded(ToolContext context) => true;

        public override async Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            var result = await context.RunAsync("xcodebuild", ["-version"], cancellationToken);
            return result.Succeeded ? ToolStatus.Installed(ParseVersion(result.Output)?.ToString()) : ToolStatus.Missing();
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => null;

        public override string? ManualInstructions(ToolContext context) => "Install Xcode from the App Store, open it once, then run: sudo xcode-select -s /Applications/Xcode.app";
    }

    private sealed class VsCodeTool : Tool
    {
        public override string Id => "vscode";

        public override string Name => "VS Code";

        public override string Why(ToolContext context) => "a code editor with the C#, Aspire and AI extensions the project recommends";

        public override bool IsNeeded(ToolContext context) => context.Needs.Ide is IdeLocator.VsCode
            || (context.Needs.Ide is null && context.Environment.IsCI is false);

        public override Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdeLocator.FindVsCode(context.Environment, context.Runner) is null ? ToolStatus.Missing() : ToolStatus.Installed());
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status) => context.Environment.Os switch
        {
            HostOs.Windows => context.PackageManagers.WingetInstall(Id, "Install VS Code", "Microsoft.VisualStudioCode", admin: false),
            HostOs.MacOS => context.PackageManagers.BrewInstall(Id, "Install VS Code", "visual-studio-code", cask: true),
            _ when context.PackageManagers.Snap is not null => new ToolAction
            {
                ToolId = Id,
                Title = "Install VS Code",
                Elevation = Elevation.Sudo,
                Commands = [PackageManagers.Sudo("snap", "install", "code", "--classic")]
            },
            _ => null
        };

        public override string? ManualInstructions(ToolContext context) => "https://code.visualstudio.com/download";
    }

    private sealed class VisualStudioTool : Tool
    {
        public override string Id => "visual-studio";

        public override string Name => "Visual Studio";

        public override bool AppliesTo(ToolContext context) => context.Environment.IsWindows;

        public override string Why(ToolContext context) => "a full IDE; large download, free for individuals and small teams under its license";

        public override bool IsNeeded(ToolContext context) => context.Needs.Ide is IdeLocator.VisualStudio;

        public override Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdeLocator.FindVisualStudio(context.Environment, context.Runner) is null ? ToolStatus.Missing() : ToolStatus.Installed());
        }

        public override ToolAction? PlanInstall(ToolContext context, ToolStatus status)
        {
            var action = context.PackageManagers.WingetInstall(Id, "Install Visual Studio Community", "Microsoft.VisualStudio.Community");

            return action is null ? null : action with
            {
                Commands =
                [
                    action.Commands[0] with
                    {
                        Arguments = [.. action.Commands[0].Arguments, "--override", "--passive --wait --add Microsoft.VisualStudio.Workload.NetWeb --add Microsoft.VisualStudio.Workload.NetCrossPlat --includeRecommended"],
                        Timeout = TimeSpan.FromHours(2)
                    }
                ]
            };
        }

        public override string? ManualInstructions(ToolContext context) => "https://visualstudio.microsoft.com/downloads/";
    }
}
