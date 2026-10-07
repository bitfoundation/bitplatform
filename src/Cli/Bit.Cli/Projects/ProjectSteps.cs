using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Bit.Cli.Templates;
using Bit.Cli.Tools;
using Bit.Cli.Trust;

namespace Bit.Cli.Projects;

public sealed record PlaywrightDriver(string Node, string Cli, string? Version);

public sealed partial class ProjectSteps(CliServices cli, ProjectContext project)
{
    public const string DevelopmentCertificateSubject = "CN=AppCertificate, OU=Development";

    private IProcessRunner Runner => cli.Runner;

    private IEnumerable<string> BuildProperties => project.BuildProperties.Select(p => $"-p:{p}");

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

        if (cli.Environment.IsWindows && (await Git(["config", "--global", "--get", "core.longpaths"], cancellationToken)).Output.Trim() is not "true")
        {
            await Git(["config", "--global", "core.longpaths", "true"], cancellationToken);
        }

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
        string? identity = null;

        if (commit.Succeeded is false && (commit.Output.Contains("user.email", StringComparison.Ordinal) || commit.Output.Contains("Please tell me who you are", StringComparison.Ordinal)))
        {
            var name = cli.Environment.GetVariable("USERNAME") ?? cli.Environment.GetVariable("USER") ?? Path.GetFileName(cli.Environment.HomeDirectory.TrimEnd('/', '\\'));
            var email = $"{(GitEmailRegex().Replace(name, ".").Trim('.') is { Length: > 0 } local ? local : "user")}@git.com";
            await Git(["config", "user.name", name], cancellationToken);
            await Git(["config", "user.email", email], cancellationToken);
            commit = await Git(["commit", "-q", "-m", $"Create {project.Name} with bit new"], cancellationToken);
            identity = $"{name} <{email}>";
        }

        if (commit.Succeeded is false)
            return StepResult.FromProcess(commit, "", title, followUp);

        await Git(["branch", "main"], cancellationToken);
        project.GitReady = true;

        return identity is null
            ? StepResult.Succeeded("Initialized git", "develop and main")
            : StepResult.Succeeded("Initialized git", $"develop and main, committed as {identity}", "git didn't know your name, so this repository uses your user name; change it with git config user.name and user.email");
    }

    public async Task<StepResult> SdkAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        if (TemplateRequirements.FromProject(project.Directory, project.Name).Sdk is not { } required)
            return StepResult.Skipped("Skipped the .NET SDK check", "the project has no global.json");

        if (await ToolCatalog.ResolveSdkAsync(Runner, project.Directory, cancellationToken) is { } version)
            return StepResult.Succeeded("The .NET SDK is ready", version);

        return StepResult.Failed("The project's .NET SDK isn't installed", $"global.json asks for {(required.Pinned ? "exactly " : "")}{required.Version}",
            $"cd {ProcessSpec.Quote(project.Directory)} && bit setup", resultCode: "sdk.missing");
    }

    public async Task<StepResult> WorkloadsAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var needed = Templates.Platforms.Workloads(project.Platforms, cli.Environment.Os);
        var list = await Dotnet(["workload", "list"], cancellationToken, progress, TimeSpan.FromMinutes(2));
        var installed = ParseInstalledWorkloads(list.Output);
        var missing = needed.Where(w => installed.Contains(w) is false).ToArray();

        if (list.Succeeded && missing.Length == 0)
            return StepResult.Succeeded("Build tools already installed", string.Join(", ", needed));

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
                return StepResult.Warning("Build tools need sudo", string.Join(", ", toInstall), followUp.Replace("dotnet workload", "sudo dotnet workload", StringComparison.Ordinal));
            }
        }

        return StepResult.FromProcess(result, "Installed build tools", "Couldn't install build tools", followUp, string.Join(", ", toInstall));
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
        string[] arguments = ["restore", project.BuildPath, .. BuildProperties];
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(30));
        return StepResult.FromProcess(result, "Restored NuGet packages", "Couldn't restore NuGet packages", $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}");
    }

    public async Task<StepResult> AndroidDependenciesAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = ["build", Templates.Platforms.MauiProject(project.Name), "-t:InstallAndroidDependencies", "-f", $"{project.TargetFrameworkVersion}-android", "-p:AcceptAndroidSDKLicenses=True"];
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(60));

        return StepResult.FromProcess(result, "Installed the Android SDK and Java", "Couldn't install the Android SDK and Java",
            $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}");
    }

    public async Task<StepResult> XcodeAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var sdk = await Dotnet(["msbuild", Templates.Platforms.MauiProject(project.Name), $"-p:TargetFramework={project.TargetFrameworkVersion}-ios", "-getProperty:_XamarinSdkRootDirectory"], cancellationToken, progress, TimeSpan.FromMinutes(5));
        var sdkRoot = sdk.Succeeded ? sdk.OutputLines.Select(l => l.Trim()).LastOrDefault(Path.IsPathRooted) : null;
        var versions = sdkRoot is null ? null : Path.Combine(sdkRoot, "Versions.plist");
        var wanted = versions is not null && File.Exists(versions) ? RecommendedXcodeRegex().Match(File.ReadAllText(versions)) : null;

        if (wanted is not { Success: true })
            return StepResult.Skipped("Skipped the Xcode check", ".NET for iOS doesn't say which Xcode it wants");

        var xcode = await Runner.RunAsync(new ProcessSpec { FileName = "xcodebuild", Arguments = ["-version"], Timeout = TimeSpan.FromMinutes(1) }, cancellationToken);
        var installed = xcode.Succeeded ? XcodeVersionRegex().Match(xcode.Output) : null;

        if (installed is not { Success: true })
            return StepResult.Skipped("Skipped the Xcode check", "Xcode isn't installed");

        var wantedVersion = wanted.Groups["version"].Value.Trim();
        var installedVersion = installed.Groups["version"].Value;

        return MajorMinor(installedVersion) == MajorMinor(wantedVersion)
            ? StepResult.Succeeded("Xcode matches .NET for iOS", $"Xcode {installedVersion}")
            : StepResult.Warning("Xcode doesn't match .NET for iOS", $"{installedVersion} here, {wantedVersion} wanted", hint: $"Get Xcode {wantedVersion} at https://developer.apple.com/download/all", resultCode: "xcode.mismatch");
    }

    public async Task<StepResult> BuildAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = ["build", project.BuildPath, .. BuildProperties];
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(60));
        var what = project.BuildsSolution ? "the solution" : "the web app";
        string? detail = null;

        if (result is { Succeeded: false, NotFound: false, TimedOut: false } && DiagnosticCodes.Extract(result.Output, int.MaxValue).Any(c => c.StartsWith("RZ", StringComparison.Ordinal)))
        {
            await Dotnet(["build-server", "shutdown", "--vbcscompiler"], cancellationToken, progress, TimeSpan.FromMinutes(2));
            result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(60));
            detail = "on a second try, after restarting the C# compiler server";
        }

        return StepResult.FromProcess(result, $"Built {what}", $"Couldn't build {what}",
            $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}", detail);
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

    public async Task<StepResult> AspireStartAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        const string skipped = "Skipped Aspire's first start";
        const string failed = "Couldn't start the project with Aspire";
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && aspire start";

        if (System.IO.Directory.Exists(project.AppHostDirectory) is false)
            return StepResult.Skipped(skipped, "the project has no AppHost");

        if (Runner.FindExecutable("aspire") is null)
            return StepResult.Warning(skipped, "the Aspire CLI isn't installed", followUp);

        if (await ContainerRuntimeProblemAsync(cancellationToken) is { } problem)
            return StepResult.Warning(skipped, problem.Detail, followUp, problem.Fix);

        var start = await Aspire(["start", "--apphost", project.AppHostDirectory, "--non-interactive"], progress, TimeSpan.FromMinutes(30), cancellationToken);

        if (start.Succeeded is false)
            return StepResult.FromProcess(start, "", failed, followUp) with { Status = StepStatus.Warning };

        try
        {
            foreach (var resource in AspireResourcesToWaitFor(project.AppHostDirectory))
            {
                if (await WaitUntilHealthyAsync(resource, progress, cancellationToken) is { } reason)
                    return StepResult.Warning($"Aspire started, {resource} didn't get healthy", reason, followUp);
            }
        }
        finally
        {
            await Aspire(["stop", "--apphost", project.AppHostDirectory, "--non-interactive"], progress, TimeSpan.FromMinutes(5), CancellationToken.None);
        }

        return StepResult.Succeeded("Started and stopped the project with Aspire", "its images and builds are ready, so the IDE starts it fast");
    }

    private async Task<(string Detail, string? Fix)?> ContainerRuntimeProblemAsync(CancellationToken cancellationToken)
    {
        var doctor = await Aspire(["doctor", "--format", "json", "--non-interactive", "--nologo"], _ => { }, TimeSpan.FromMinutes(2), cancellationToken);
        var checks = ReadJson(doctor.Output, root => root.TryGetProperty("checks", out var all) && all.ValueKind is JsonValueKind.Array
            ? all.EnumerateArray().Where(c => Text(c, "category") is "container").Select(c => (Status: Text(c, "status"), Message: Text(c, "message"), Fix: Text(c, "fix"))).ToList()
            : null) ?? [];

        if (checks.Count == 0)
        {
            var docker = await Runner.RunAsync(new ProcessSpec { FileName = "docker", Arguments = ["info", "--format", "{{.ServerVersion}}"], Timeout = TimeSpan.FromMinutes(1) }, cancellationToken);
            return docker.Succeeded ? null : ("Docker isn't running", null);
        }

        if (checks.Any(c => c.Status is "pass"))
            return null;

        var worst = checks.OrderBy(c => c.Status is "fail" ? 0 : 1).First();
        return (worst.Message ?? "Aspire can't use a container runtime", worst.Fix);
    }

    private async Task<string?> WaitUntilHealthyAsync(string resource, Action<string> progress, CancellationToken cancellationToken)
    {
        const int roundSeconds = 30;
        const int maxRounds = 60;
        const int unhealthyRuntimeRounds = 4;
        var runtimeUnhealthyRounds = 0;
        var pending = "";

        for (var round = 1; round <= maxRounds; round++)
        {
            var wait = await Aspire(["wait", resource, "--status", "healthy", "--timeout", $"{roundSeconds}", "--apphost", project.AppHostDirectory, "--non-interactive"], _ => { }, TimeSpan.FromSeconds(roundSeconds + 90), cancellationToken);

            if (wait.Succeeded)
                return null;

            var resources = await DescribeAspireResourcesAsync(cancellationToken);
            var failed = resources.Where(r => r.State is "FailedToStart" || (r.State is "Exited" or "Finished" && r.ExitCode is not (null or 0))).ToList();

            if (failed.Count > 0)
                return $"{Join(failed.Select(r => r.Name))} failed to start";

            if (wait.ExitCode is not AspireWaitTimedOut || wait.TimedOut)
                return DiagnosticCodes.FirstErrorLine(wait.Output) ?? $"aspire wait {resource} ended with exit code {wait.ExitCode}";

            var unhealthy = resources.Where(r => r.State is "RuntimeUnhealthy").ToList();
            runtimeUnhealthyRounds = unhealthy.Count > 0 ? runtimeUnhealthyRounds + 1 : 0;

            if (runtimeUnhealthyRounds >= unhealthyRuntimeRounds)
                return $"Docker can't run {Join(unhealthy.Select(r => r.Name))}: Aspire reports its container runtime as unhealthy";

            pending = Join(resources.Where(r => r.State is not "Running" || r.HealthStatus is not (null or "Healthy")).Take(4).Select(r => r.HealthStatus is null or "Healthy" ? $"{r.Name} {r.State}" : $"{r.Name} {r.State} ({r.HealthStatus})"));
            progress(pending.Length > 0 ? $"Waiting for {resource}: {pending}" : $"Waiting for {resource}");
        }

        return pending.Length > 0 ? $"still waiting after {maxRounds * roundSeconds / 60} minutes on {pending}" : $"not healthy after {maxRounds * roundSeconds / 60} minutes";
    }

    private const int AspireWaitTimedOut = 17;

    private async Task<IReadOnlyList<(string Name, string? State, string? HealthStatus, int? ExitCode)>> DescribeAspireResourcesAsync(CancellationToken cancellationToken)
    {
        var describe = await Aspire(["describe", "--format", "json", "--apphost", project.AppHostDirectory, "--non-interactive", "--nologo"], _ => { }, TimeSpan.FromMinutes(1), cancellationToken);

        return ReadJson(describe.Output, root => root.TryGetProperty("resources", out var all) && all.ValueKind is JsonValueKind.Array
            ? all.EnumerateArray().Select(r => (Name: Text(r, "displayName") ?? Text(r, "name") ?? "?", State: Text(r, "state"), HealthStatus: Text(r, "healthStatus"), ExitCode: r.TryGetProperty("exitCode", out var code) && code.ValueKind is JsonValueKind.Number ? code.GetInt32() : (int?)null)).ToList()
            : null) ?? [];
    }

    private static T? ReadJson<T>(string output, Func<JsonElement, T?> read) where T : class
    {
        var start = output.IndexOf('{');
        var end = output.LastIndexOf('}');

        if (start < 0 || end <= start)
            return null;

        try
        {
            using var document = JsonDocument.Parse(output[start..(end + 1)]);
            return read(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Text(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value.ValueKind is JsonValueKind.String ? property.Value.GetString() : null;
        }

        return null;
    }

    private static string Join(IEnumerable<string> items) => string.Join(", ", items);

    public static IReadOnlyList<string> AspireResourcesToWaitFor(string appHostDirectory)
    {
        var program = Path.Combine(appHostDirectory, "Program.cs");
        var text = File.Exists(program) ? File.ReadAllText(program) : "";

        return [.. new[] { "serverweb", "serverapi" }.Where(resource => text.Contains($"\"{resource}\"", StringComparison.Ordinal))];
    }

    private Task<ProcessResult> Aspire(string[] arguments, Action<string> progress, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return Runner.RunAsync(new ProcessSpec { FileName = "aspire", Arguments = arguments, WorkingDirectory = project.Directory, OnOutputLine = progress, Timeout = timeout }, cancellationToken);
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

    public async Task<StepResult> PlaywrightAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var everyBrowser = cli.Environment.IsCI;
        var title = everyBrowser ? "Installed Playwright's browsers" : "Installed Chromium for UI tests";
        string[] browsers = everyBrowser ? [] : ["chromium"];

        if (FindPlaywrightDriver(project.Directory, cli.Environment.Os) is not { } driver)
            return StepResult.Skipped(everyBrowser ? "Skipped Playwright's browsers" : "Skipped Playwright's browser", "the UI tests weren't built");

        var install = new ProcessSpec { FileName = driver.Node, Arguments = [driver.Cli, "install", .. browsers], WorkingDirectory = project.Directory, OnOutputLine = progress, Timeout = TimeSpan.FromMinutes(30) };
        var result = await Runner.RunAsync(install, cancellationToken);
        var detail = driver.Version is null ? null : $"Playwright {driver.Version}";

        if (result.Succeeded is false)
            return StepResult.FromProcess(result, "", everyBrowser ? "Couldn't install Playwright's browsers" : "Couldn't install Playwright's Chromium", install.CommandLine);

        if (cli.Environment.IsLinux is false)
            return StepResult.Succeeded(title, detail);

        var dependencies = install with { Arguments = [driver.Cli, "install-deps", .. browsers], OnOutputLine = progress };
        var followUp = "sudo " + dependencies.CommandLine;

        if (cli.Environment.IsElevated is false)
        {
            if (Runner.FindExecutable("sudo") is null || (await Runner.RunAsync(new ProcessSpec { FileName = "sudo", Arguments = ["-n", "true"], Timeout = TimeSpan.FromSeconds(10) }, cancellationToken)).Succeeded is false)
                return StepResult.Warning(title, "its system libraries need sudo", followUp);

            dependencies = dependencies with { FileName = "sudo", Arguments = ["-n", driver.Node, .. dependencies.Arguments] };
        }

        var installed = await Runner.RunAsync(dependencies, cancellationToken);
        return installed.Succeeded ? StepResult.Succeeded(title, detail) : StepResult.Warning(title, "its system libraries weren't installed", followUp);
    }

    public static PlaywrightDriver? FindPlaywrightDriver(string projectDirectory, HostOs os)
    {
        var bin = Path.Combine(projectDirectory, "src", "Tests", "bin");

        if (System.IO.Directory.Exists(bin) is false)
            return null;

        foreach (var playwright in System.IO.Directory.EnumerateDirectories(bin, ".playwright", SearchOption.AllDirectories).OrderByDescending(System.IO.Directory.GetLastWriteTimeUtc))
        {
            var cliScript = Path.Combine(playwright, "package", "cli.js");
            var nodeDirectory = Path.Combine(playwright, "node");

            if (File.Exists(cliScript) is false || System.IO.Directory.Exists(nodeDirectory) is false)
                continue;

            var node = System.IO.Directory.EnumerateFiles(nodeDirectory, os is HostOs.Windows ? "node.exe" : "node", SearchOption.AllDirectories).FirstOrDefault();

            if (node is not null)
                return new PlaywrightDriver(node, cliScript, ReadPackageVersion(Path.Combine(playwright, "package", "package.json")));
        }

        return null;
    }

    public async Task<StepResult> VsCodeExtensionsAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var recommended = ReadRecommendedExtensions(project.Directory);

        if (recommended.Count == 0)
            return StepResult.Skipped("Skipped VS Code extensions", "the project recommends none");

        if (IdeLocator.FindVsCode(cli.Environment, Runner) is not { } code)
            return StepResult.Skipped("Skipped VS Code extensions", "VS Code isn't installed");

        var listed = await Runner.RunAsync(new ProcessSpec { FileName = code.Executable, Arguments = ["--list-extensions"], Timeout = TimeSpan.FromMinutes(2) }, cancellationToken);
        var installed = listed.Succeeded ? listed.OutputLines.Select(l => l.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase) : [];
        var missing = recommended.Where(id => installed.Contains(id) is false).ToList();

        if (listed.Succeeded && missing.Count == 0)
            return StepResult.Succeeded("Extensions already installed", $"{recommended.Count} recommended for VS Code");

        string[] arguments = [.. missing.SelectMany(id => new[] { "--install-extension", id })];
        var install = new ProcessSpec { FileName = code.Executable, Arguments = arguments, OnOutputLine = progress, Timeout = TimeSpan.FromMinutes(15) };
        var result = await Runner.RunAsync(install, cancellationToken);
        var builtIn = BuiltInExtensionRegex().Matches(result.Output).Select(m => m.Groups["id"].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (result.Succeeded is false && builtIn.Count > 0)
        {
            missing = [.. missing.Where(id => builtIn.Contains(id) is false)];

            if (missing.Count == 0)
                return StepResult.Succeeded("Extensions already installed", $"{recommended.Count} recommended for VS Code");

            arguments = [.. missing.SelectMany(id => new[] { "--install-extension", id })];
            install = install with { Arguments = arguments };
            result = await Runner.RunAsync(install, cancellationToken);
        }

        var names = string.Join(", ", missing.Take(3).Select(id => id[(id.IndexOf('.') + 1)..])) + (missing.Count > 3 ? $" and {missing.Count - 3} more" : "");

        return StepResult.FromProcess(result, $"Installed {missing.Count} VS Code extension{(missing.Count == 1 ? "" : "s")}", "Couldn't install the VS Code extensions",
            $"{ProcessSpec.Quote(code.Executable)} {string.Join(' ', arguments)}", names);
    }

    public static IReadOnlyList<string> ReadRecommendedExtensions(string projectDirectory)
    {
        var path = Path.Combine(projectDirectory, ".vscode", "extensions.json");

        if (File.Exists(path) is false)
            return [];

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            return document.RootElement.ValueKind is JsonValueKind.Object && document.RootElement.TryGetProperty("recommendations", out var recommendations) && recommendations.ValueKind is JsonValueKind.Array
                ? [.. recommendations.EnumerateArray().Where(e => e.ValueKind is JsonValueKind.String).Select(e => e.GetString()!.Trim()).Where(id => id.Contains('.')).Distinct(StringComparer.OrdinalIgnoreCase)]
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ReadPackageVersion(string packageJson)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(packageJson));
            return document.RootElement.TryGetProperty("version", out var version) ? version.GetString() : null;
        }
        catch (Exception exp) when (exp is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
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

        return Runner.StartDetached(spec)
            ? StepResult.Succeeded($"Opened {found.Name}")
            : StepResult.Failed($"Couldn't open {found.Name}", $"{found.Executable} didn't start", spec.CommandLine);
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

    public async Task<bool> CanSignInToGitHubAsync(CancellationToken cancellationToken)
    {
        if (cli.HasTerminal is false || Runner.FindExecutable("gh") is null)
            return false;

        return (await Gh(["auth", "status"], cancellationToken)).Succeeded is false;
    }

    public async Task<StepResult> GitHubSignInAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var login = await Runner.RunAsync(new ProcessSpec
        {
            FileName = "gh",
            Arguments = ["auth", "login", "--web", "--git-protocol", "https", "--hostname", "github.com"],
            WorkingDirectory = project.Directory,
            Interactive = true,
            Timeout = TimeSpan.FromMinutes(15)
        }, cancellationToken);

        return login.Succeeded
            ? StepResult.Succeeded("Signed in to GitHub")
            : StepResult.Warning("Not signed in to GitHub", "the sign-in didn't finish", "gh auth login --web");
    }

    public async Task<StepResult> GitHubRepositoryAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        const string title = "Couldn't create the GitHub repo";
        var push = "git push -u origin develop main && gh repo edit --default-branch develop";
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && gh repo create {ProcessSpec.Quote(project.Name)} --private --source . --remote origin && gh auth setup-git && {push}";

        if (project.GitReady is false)
            return StepResult.Skipped("Skipped the GitHub repo", "git has nothing committed to push");

        if (Runner.FindExecutable("gh") is null)
            return StepResult.Failed(title, "the GitHub CLI isn't installed", followUp, resultCode: "tool.gh.missing", hint: "Install it from https://cli.github.com");

        if ((await Gh(["auth", "status"], cancellationToken)).Succeeded is false)
            return StepResult.Warning("Didn't create the GitHub repo", "you aren't signed in to GitHub", $"gh auth login --web && {followUp}", resultCode: "github.signin");

        var create = await Gh(["repo", "create", project.Name, "--private", "--source", ".", "--remote", "origin"], cancellationToken, progress);

        if (create.Succeeded is false)
            return StepResult.FromProcess(create, "", title, followUp);

        await Gh(["auth", "setup-git"], cancellationToken);
        var pushed = await Git(["push", "-u", "origin", "develop", "main"], cancellationToken);

        if (pushed.Succeeded is false)
            return StepResult.FromProcess(pushed, "", "Couldn't push to the GitHub repo", $"cd {ProcessSpec.Quote(project.Directory)} && {push}");

        await Gh(["repo", "edit", "--default-branch", "develop"], cancellationToken);
        var url = await Gh(["repo", "view", "--json", "url", "--jq", ".url"], cancellationToken);

        return StepResult.Succeeded("Created a private GitHub repo", url.Succeeded && url.Output.Trim().Length > 0 ? url.Output.Trim().Replace("https://", "", StringComparison.Ordinal) : "develop and main pushed");
    }

    private async Task<string?> CommitPathAsync(string path, string message, CancellationToken cancellationToken)
    {
        if (project.GitReady is false)
            return null;

        await Git(["add", "--", path], cancellationToken);
        var commit = await Git(["commit", "-q", "-m", message, "--", path], cancellationToken);
        return commit.Succeeded ? "committed" : null;
    }

    private Task<ProcessResult> Gh(string[] arguments, CancellationToken cancellationToken, Action<string>? progress = null)
    {
        return Runner.RunAsync(new ProcessSpec { FileName = "gh", Arguments = arguments, WorkingDirectory = project.Directory, OnOutputLine = progress, Timeout = TimeSpan.FromMinutes(5) }, cancellationToken);
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

    private static string MajorMinor(string version) => string.Join('.', version.Split('.').Take(2));

    [GeneratedRegex(@"Extension '(?<id>[^']+)' is a built-in extension")]
    private static partial Regex BuiltInExtensionRegex();

    [GeneratedRegex(@"<key>RecommendedXcodeVersion</key>\s*<string>(?<version>[^<]+)</string>")]
    private static partial Regex RecommendedXcodeRegex();

    [GeneratedRegex(@"Xcode\s+(?<version>\d+\.\d+(\.\d+)?)")]
    private static partial Regex XcodeVersionRegex();

    [GeneratedRegex(@"[^A-Za-z0-9._-]+")]
    private static partial Regex GitEmailRegex();
}
