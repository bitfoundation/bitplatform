using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Bit.Cli.Tools;
using Bit.Cli.Trust;

namespace Bit.Cli.Projects;

public sealed class ProjectSteps(CliServices cli, ProjectContext project)
{
    public const string DevelopmentCertificateSubject = "CN=AppCertificate, OU=Development";

    private IProcessRunner Runner => cli.Runner;

    public Task<StepResult> AppCertificateAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        if (System.IO.Directory.Exists(project.ServerApiDirectory) is false)
            return Task.FromResult(StepResult.Skipped("Skipped the app certificate", "the project has no Server.Api folder"));

        WriteDevelopmentCertificate(project.ServerApiDirectory);
        return Task.FromResult(StepResult.Succeeded("Created a unique app certificate", "development only"));
    }

    public static void WriteDevelopmentCertificate(string directory)
    {
        using var rsa = RSA.Create(3072);
        var request = new CertificateRequest(DevelopmentCertificateSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));

        AtomicFile.WriteAllText(Path.Combine(directory, "AppCertificate.crt"), certificate.ExportCertificatePem().ReplaceLineEndings("\n") + "\n");
        AtomicFile.WriteAllText(Path.Combine(directory, "AppCertificate.key"), rsa.ExportPkcs8PrivateKeyPem().ReplaceLineEndings("\n") + "\n");
    }

    public async Task<StepResult> GitAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        const string title = "Couldn't set up git";
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && git init -b develop && git add -A && git commit -m \"Create {project.Name} with bit new\" && git branch main";

        if (Runner.FindExecutable("git") is null)
            return StepResult.Failed(title, "git isn't installed", followUp, resultCode: "tool.git.missing");

        var parentRepository = await Git(["rev-parse", "--show-toplevel"], cancellationToken, Path.GetDirectoryName(project.Directory));

        if (parentRepository.Succeeded)
            return StepResult.Skipped("Skipped git", $"the project is inside {parentRepository.Output.Trim()}, an existing repository");

        var init = await Git(["init", "-b", "develop"], cancellationToken);

        if (init.Succeeded is false)
        {
            init = await Git(["init"], cancellationToken);

            if (init.Succeeded)
            {
                await Git(["symbolic-ref", "HEAD", "refs/heads/develop"], cancellationToken);
            }
        }

        if (init.Succeeded is false)
            return StepResult.FromProcess(init, "", title, followUp);

        await Git(["add", "-A"], cancellationToken);
        var commit = await Git(["commit", "-q", "-m", $"Create {project.Name} with bit new"], cancellationToken);

        if (commit.Succeeded is false)
        {
            var identityMissing = commit.Output.Contains("user.email", StringComparison.Ordinal) || commit.Output.Contains("Please tell me who you are", StringComparison.Ordinal);

            if (identityMissing is false)
                return StepResult.FromProcess(commit, "", title, followUp);

            var identityFollowUp = $"git config --global user.name \"Your Name\" && git config --global user.email you@example.com && cd {ProcessSpec.Quote(project.Directory)} && git commit -m \"Create {project.Name} with bit new\" && git branch main";
            return StepResult.Warning("Initialized git, nothing committed", "git doesn't know your name and email yet", identityFollowUp, resultCode: "git.identity.missing");
        }

        await Git(["branch", "main"], cancellationToken);
        project.GitReady = true;

        return StepResult.Succeeded("Initialized git", "develop and main");
    }

    public async Task<StepResult> WorkloadsAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var needed = Templates.Platforms.Workloads(project.Platforms, cli.Environment.Os);
        var list = await Dotnet(["workload", "list"], cancellationToken, progress, TimeSpan.FromMinutes(2));
        var installed = ParseInstalledWorkloads(list.Output);
        var missing = needed.Where(w => installed.Contains(w) is false).ToArray();

        if (list.Succeeded && missing.Length == 0)
            return StepResult.Succeeded("Workloads already installed", string.Join(", ", needed));

        var toInstall = list.Succeeded ? missing : [.. needed];
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && dotnet workload install {string.Join(' ', toInstall)}";
        var result = await Dotnet(["workload", "install", .. toInstall], cancellationToken, progress, TimeSpan.FromMinutes(60));

        if (result.Succeeded is false && cli.Environment.IsWindows is false && NeedsElevation(result.Output) && cli.Environment.IsElevated is false && cli.Runner.FindExecutable("sudo") is not null)
        {
            var sudoReady = (await Runner.RunAsync(new ProcessSpec { FileName = "sudo", Arguments = ["-n", "true"] }, cancellationToken)).Succeeded;

            if (sudoReady)
            {
                result = await Runner.RunAsync(new ProcessSpec
                {
                    FileName = "sudo",
                    Arguments = ["-n", "dotnet", "workload", "install", .. toInstall],
                    WorkingDirectory = project.Directory,
                    OnOutputLine = progress,
                    Timeout = TimeSpan.FromMinutes(60)
                }, cancellationToken);
            }
            else
            {
                return StepResult.Warning("Workloads need sudo", string.Join(", ", toInstall), followUp.Replace("dotnet workload", "sudo dotnet workload", StringComparison.Ordinal));
            }
        }

        return StepResult.FromProcess(result, "Installed workloads", "Couldn't install workloads", followUp, string.Join(", ", toInstall));
    }

    public static IReadOnlySet<string> ParseInstalledWorkloads(string output)
    {
        var lines = output.Split('\n').Select(l => l.Trim()).ToArray();
        var start = Array.FindIndex(lines, l => l.StartsWith("---", StringComparison.Ordinal));

        if (start < 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return lines.Skip(start + 1)
            .TakeWhile(l => l.Length > 0 && l.StartsWith("Use `", StringComparison.Ordinal) is false)
            .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<StepResult> RestoreAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var result = await Dotnet(["restore", Path.GetFileName(project.WebSolutionFilter)], cancellationToken, progress, TimeSpan.FromMinutes(30));
        return StepResult.FromProcess(result, "Restored NuGet packages", "Couldn't restore NuGet packages", $"cd {ProcessSpec.Quote(project.Directory)} && dotnet restore {Path.GetFileName(project.WebSolutionFilter)}");
    }

    public async Task<StepResult> AndroidDependenciesAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var target = Templates.Platforms.BuildTargets(project.Name, [Platform.Android], project.TargetFrameworkVersion)[0];
        string[] arguments = ["build", target.ProjectPath, "-t:InstallAndroidDependencies", "-f", target.TargetFramework!, "-p:AcceptAndroidSDKLicenses=True"];
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(60));

        return StepResult.FromProcess(result, "Installed the Android SDK and Java", "Couldn't install the Android SDK and Java",
            $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}");
    }

    public async Task<StepResult> BuildAsync(BuildTarget target, Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = target.TargetFramework is null ? ["build", target.ProjectPath] : ["build", target.ProjectPath, "-f", target.TargetFramework];
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(60));
        var platform = Templates.Platforms.Title(target.Platform);

        return StepResult.FromProcess(result, $"Built the {platform} app", $"Couldn't build the {platform} app",
            $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}");
    }

    public async Task<StepResult> FormatAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = ["format", Path.GetFileName(project.WebSolutionFilter), "--exclude-diagnostics", "BL0016"];
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(30));
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments)}";

        if (result.Succeeded is false)
            return StepResult.FromProcess(result, "", "Couldn't format the code", followUp);

        return StepResult.Succeeded("Formatted the code", await CommitAllAsync("Format the code with dotnet format", cancellationToken));
    }

    public async Task<StepResult> MigrationAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        if (project.Database is "Other")
            return StepResult.Skipped("Skipped the initial migration", "--database Other: add your provider, then your first migration");

        if (System.IO.Directory.Exists(project.ServerApiDirectory) is false)
            return StepResult.Skipped("Skipped the initial migration", "the project has no Server.Api folder");

        var migrations = Path.Combine(project.ServerApiDirectory, "Infrastructure", "Data", "Migrations");

        if (System.IO.Directory.Exists(migrations) && System.IO.Directory.EnumerateFiles(migrations, "*.cs").Any())
            return StepResult.Skipped("Skipped the initial migration", "the project already has migrations");

        var version = project.PackageVersion("Microsoft.EntityFrameworkCore.Design") ?? project.PackageVersion("Microsoft.EntityFrameworkCore");
        var tool = version is null ? "dotnet-ef" : $"dotnet-ef@{version}";
        string[] arguments = ["dnx", tool, "--", "migrations", "add", "Initial", "--output-dir", "Infrastructure/Data/Migrations"];
        var followUp = $"cd {ProcessSpec.Quote(project.ServerApiDirectory)} && dotnet {string.Join(' ', arguments)}";

        var result = await Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = project.ServerApiDirectory,
            OnOutputLine = progress,
            Timeout = TimeSpan.FromMinutes(30)
        }, cancellationToken);

        if (result.Succeeded is false)
            return StepResult.FromProcess(result, "", "Couldn't add the initial migration", followUp);

        var committed = await CommitPathAsync(migrations, "Add the Initial EF Core migration", cancellationToken);
        return StepResult.Succeeded("Added the Initial migration", $"{project.Database}{(committed is null ? "" : ", committed")}");
    }

    public Task<StepResult> TrustAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var context = new TrustContext(cli.Environment, Runner);
        var outcomes = TrustWriters.All.Select(writer =>
        {
            var outcome = writer.Trust(context, project.Directory);
            cli.Log.Write($"trust {outcome.Tool}: {outcome.Kind} {outcome.Detail}");
            return outcome;
        }).ToList();

        var trusted = outcomes.Where(o => o.Kind is TrustResultKind.Trusted or TrustResultKind.AlreadyTrusted).Select(o => o.Tool).ToList();
        var notes = outcomes.Where(o => o.Kind is TrustResultKind.Skipped or TrustResultKind.Failed).Select(o => $"{o.Tool}: {o.Detail}").ToList();

        if (trusted.Count == 0 && notes.Count == 0)
            return Task.FromResult(StepResult.Skipped("Skipped trust", "none of VS Code, Claude Code, Copilot CLI, Codex or Gemini CLI is installed"));

        var title = trusted.Count == 0 ? "Couldn't trust the folder" : $"Trusted for {string.Join(" · ", trusted)}";

        return Task.FromResult(notes.Count == 0
            ? StepResult.Succeeded(title)
            : StepResult.Warning(title, string.Join("; ", notes), hint: outcomes.Any(o => o.Tool is "VS Code" && o.Kind is not (TrustResultKind.Trusted or TrustResultKind.AlreadyTrusted))
                ? "Close VS Code and run bit trust in the project folder, or answer VS Code's trust prompt."
                : null));
    }

    public async Task<StepResult> OpenIdeAsync(string ide, Action<string> progress, CancellationToken cancellationToken)
    {
        var found = IdeLocator.Find(ide, cli.Environment, Runner);

        if (found is null)
            return StepResult.Warning("Didn't open an IDE", $"{ide} wasn't found");

        ProcessSpec spec = ide switch
        {
            IdeLocator.VsCode => new ProcessSpec
            {
                FileName = found.Executable,
                Arguments = ["--disable-workspace-trust", project.Directory, Path.Combine(project.Directory, "README.md")]
            },
            IdeLocator.VisualStudio => new ProcessSpec { FileName = found.Executable, Arguments = [File.Exists(project.Solution) ? project.Solution : project.WebSolutionFilter] },
            _ => new ProcessSpec { FileName = found.Executable, Arguments = [File.Exists(project.Solution) ? project.Solution : project.Directory] }
        };

        if (ide is IdeLocator.VsCode)
        {
            var result = await Runner.RunAsync(spec with { Timeout = TimeSpan.FromMinutes(1) }, cancellationToken);
            return result.Succeeded ? StepResult.Succeeded($"Opened {found.Name}") : StepResult.FromProcess(result, "", $"Couldn't open {found.Name}", spec.CommandLine);
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(spec.FileName, spec.Arguments) { UseShellExecute = false })?.Dispose();
            return StepResult.Succeeded($"Opened {found.Name}");
        }
        catch (System.ComponentModel.Win32Exception exp)
        {
            return StepResult.Failed($"Couldn't open {found.Name}", exp.Message, spec.CommandLine);
        }
    }

    private async Task<string?> CommitAllAsync(string message, CancellationToken cancellationToken)
    {
        if (project.GitReady is false)
            return null;

        var status = await Git(["status", "--porcelain", "--untracked-files=no"], cancellationToken);

        if (status.Succeeded is false)
            return null;

        if (string.IsNullOrWhiteSpace(status.Output))
            return "nothing to change";

        var commit = await Git(["commit", "-q", "-a", "-m", message], cancellationToken);
        return commit.Succeeded ? "committed" : null;
    }

    private async Task<string?> CommitPathAsync(string path, string message, CancellationToken cancellationToken)
    {
        if (project.GitReady is false)
            return null;

        await Git(["add", "--", path], cancellationToken);
        var commit = await Git(["commit", "-q", "-m", message, "--", path], cancellationToken);
        return commit.Succeeded ? "committed" : null;
    }

    private Task<ProcessResult> Git(string[] arguments, CancellationToken cancellationToken, string? workingDirectory = null)
    {
        return Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = arguments, WorkingDirectory = workingDirectory ?? project.Directory, Timeout = TimeSpan.FromMinutes(5) }, cancellationToken);
    }

    private Task<ProcessResult> Dotnet(string[] arguments, CancellationToken cancellationToken, Action<string>? progress, TimeSpan timeout)
    {
        return Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = project.Directory,
            OnOutputLine = progress,
            Timeout = timeout,
            Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1", ["MSBUILDTERMINALLOGGER"] = "off" }
        }, cancellationToken);
    }

    private static bool NeedsElevation(string output)
    {
        return output.Contains("Access to the path", StringComparison.OrdinalIgnoreCase)
            || output.Contains("permission denied", StringComparison.OrdinalIgnoreCase)
            || output.Contains("EACCES", StringComparison.Ordinal)
            || output.Contains("Inadequate permissions", StringComparison.OrdinalIgnoreCase)
            || output.Contains("elevat", StringComparison.OrdinalIgnoreCase);
    }
}
