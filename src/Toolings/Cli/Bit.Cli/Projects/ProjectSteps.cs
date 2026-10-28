using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Bit.Cli.Templates;
using Bit.Cli.Tools;
using Bit.Cli.Trust;
using Spectre.Console;

namespace Bit.Cli.Projects;

public sealed record PlaywrightDriver(string Node, string Cli, string? Version);

public sealed class RunningProject
{
    public string? DashboardUrl { get; init; }

    public string? AppUrl { get; init; }

    public required Func<bool, Task> StopAsync { get; init; }

    public Task? Exited { get; init; }
}

public sealed partial class ProjectSteps(CliServices cli, ProjectContext project)
{
    public const string DevelopmentCertificateSubject = "CN=AppCertificate, OU=Development";

    private IProcessRunner Runner => cli.Runner;

    private Dictionary<string, string?> GitWithoutAutoMaintenance
    {
        get
        {
            var inherited = int.TryParse(cli.Environment.GetVariable("GIT_CONFIG_COUNT"), out var count) && count > 0 ? count : 0;

            return new()
            {
                ["GIT_CONFIG_COUNT"] = $"{inherited + 2}",
                [$"GIT_CONFIG_KEY_{inherited}"] = "maintenance.auto",
                [$"GIT_CONFIG_VALUE_{inherited}"] = "false",
                [$"GIT_CONFIG_KEY_{inherited + 1}"] = "gc.auto",
                [$"GIT_CONFIG_VALUE_{inherited + 1}"] = "0"
            };
        }
    }

    private IEnumerable<string> BuildProperties => project.BuildProperties.Select(p => $"-p:{p}");

    private bool migrationAdded;

    public Task<StepResult> AppCertificateAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        if (System.IO.Directory.Exists(project.ServerApiDirectory) is false)
            return Task.FromResult(StepResult.Skipped("Skipped the app certificate", "the project has no Server.Api folder"));

        WriteDevelopmentCertificate(project.ServerApiDirectory);
        return Task.FromResult(StepResult.Succeeded("Created a unique app certificate", "development only"));
    }

    public static void WriteDevelopmentCertificate(string directory) => Keys.AppCertificates.Write(directory, Keys.AppCertificates.ActiveName, Keys.AppCertificates.CreateDevelopment());

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

    public async Task<StepResult> ContainerRuntimeAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        if (await ToolCatalog.ContainerRuntimeAsync(Runner, cli.Environment, cancellationToken) is not { } runtime)
            return StepResult.Skipped("Skipped the container runtime", "neither Docker nor Podman answers");

        var name = runtime is "podman" ? "Podman" : "Docker";
        string[] arguments = ["user-secrets", "set", "DcpPublisher:ContainerRuntime", runtime, "--project", project.AppHostDirectory];
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(2));

        return StepResult.FromProcess(result, $"Aspire runs the containers on {name}", $"Couldn't tell Aspire to use {name}",
            $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}", "kept in the AppHost's user secrets");
    }

    public async Task<StepResult> WorkloadsAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var support = Templates.Platforms.BuildSupport(project.Platforms);
        var needed = Templates.Platforms.Workloads(project.Platforms, cli.Environment.Os);
        var list = await Dotnet(["workload", "list"], cancellationToken, progress, TimeSpan.FromMinutes(2));
        var installed = ParseInstalledWorkloads(list.Output);
        var missing = needed.Where(w => installed.Contains(w) is false).ToArray();

        if (list.Succeeded && missing.Length == 0)
            return StepResult.Succeeded($"{support} is installed", string.Join(", ", needed));

        var toInstall = list.Succeeded ? missing : [.. needed];
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && dotnet workload install {string.Join(' ', toInstall)}";
        var administrator = cli.Environment.IsWindows && AdministratorNeeds.DotnetNeedsIt(cli) ? cli.Administrator : null;
        var result = await Dotnet(["workload", "install", .. toInstall], cancellationToken, progress, TimeSpan.FromMinutes(60), administrator);

        if (result.Succeeded is false && cli.Environment.IsWindows is false && NeedsElevation(result.Output) && cli.Environment.IsElevated is false && cli.Runner.FindExecutable("sudo") is not null)
        {
            var sudoReady = (await Runner.RunAsync(new ProcessSpec { FileName = "sudo", Arguments = ["-n", "true"] }, cancellationToken)).Succeeded;

            if (sudoReady)
            {
                result = await Runner.RunAsync(new ProcessSpec
                {
                    FileName = "dotnet",
                    Arguments = ["workload", "install", .. toInstall],
                    WorkingDirectory = project.Directory,
                    OnOutputLine = progress
                }.WithSudo(Runner), cancellationToken);
            }
            else
            {
                return StepResult.Warning($"{support} needs sudo", string.Join(", ", toInstall), followUp.Replace("dotnet workload", "sudo dotnet workload", StringComparison.Ordinal));
            }
        }

        return StepResult.FromProcess(result, $"Installed {support}", $"Couldn't install {support}", followUp, string.Join(", ", toInstall));
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
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}";
        var administrator = cli.Environment.IsWindows && AdministratorNeeds.AndroidSdkNeedsIt(cli.Environment) ? cli.Administrator : null;
        var result = await Dotnet(arguments, cancellationToken, progress, TimeSpan.FromMinutes(60), administrator);

        if (result.Succeeded is false && administrator is null && cli.Environment.IsWindows && cli.Environment.IsElevated is false && NeedsElevation(result.Output))
        {
            if (cli.AdministratorDeclined)
                return StepResult.Warning("The Android SDK and Java weren't installed", "administrator permission was declined", followUp);

            if (ToolInstaller.CanRunAsAdministrator(cli) is false)
                return StepResult.Warning("The Android SDK and Java need administrator rights", "run it in an elevated terminal", followUp);

            var install = new ToolAction
            {
                ToolId = "android",
                Title = "Install the Android SDK and Java",
                Commands = [new ProcessSpec { FileName = "dotnet", Arguments = arguments, WorkingDirectory = project.Directory, Timeout = TimeSpan.FromMinutes(60) }]
            };

            var elevated = await ToolInstaller.RunAsAdministratorAsync(cli, [install], progress, cancellationToken);

            if (elevated.Declined)
                return StepResult.Warning("The Android SDK and Java weren't installed", "administrator permission was declined", followUp);

            return elevated.ExitCodes.TryGetValue(install.ToolId, out var exitCode) && exitCode == 0
                ? StepResult.Succeeded("Installed the Android SDK and Java", "as administrator")
                : StepResult.Failed("Couldn't install the Android SDK and Java", "as administrator", followUp, hint: ToolInstaller.AdministratorOutput(elevated.Log, install.Title));
        }

        return StepResult.FromProcess(result, "Installed the Android SDK and Java", "Couldn't install the Android SDK and Java", followUp);
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

        migrationAdded = true;
        var committed = await CommitPathAsync(migrations, "Add the Initial EF Core migration", cancellationToken);
        return StepResult.Succeeded("Added the Initial migration", $"{project.Database}{(committed is null ? "" : ", committed")}");
    }

    public RunningProject? Running { get; private set; }

    public Func<Uri, CancellationToken, Task<bool>> AliveProbe { get; init; } = IsAliveAsync;

    private string ServerWebDirectory => Path.Combine(project.Directory, "src", "Server", $"{project.Name}.Server.Web");

    public Task<StepResult> AspireStartAsync(Action<string> progress, CancellationToken cancellationToken) => AspireStartAsync(keepRunning: false, progress, cancellationToken);

    public async Task<StepResult> AspireStartAsync(bool keepRunning, Action<string> progress, CancellationToken cancellationToken)
    {
        var skipped = keepRunning ? "Didn't start the project" : "Skipped Aspire's first start";
        const string failed = "Couldn't start the project with Aspire";
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && aspire start";

        if (System.IO.Directory.Exists(project.AppHostDirectory) is false)
            return StepResult.Skipped(skipped, "the project has no AppHost");

        if (Runner.FindExecutable("aspire") is null)
            return StepResult.Warning(skipped, "the Aspire CLI isn't installed", followUp);

        var containerProblem = await ContainerRuntimeProblemAsync(cancellationToken);

        if (containerProblem is { } problem && project.NeedsContainers)
            return StepResult.Warning(skipped, problem.Detail, followUp, problem.Fix);

        if (containerProblem is { } missing && keepRunning is false)
            return StepResult.Skipped(skipped, $"there are no containers to get ready: {missing.Detail}");

        var startTimeout = cli.Environment.GetVariable("ASPIRE_CLI_START_TIMEOUT") is null
            ? new Dictionary<string, string?> { ["ASPIRE_CLI_START_TIMEOUT"] = "1200" }
            : null;

        var runningBefore = await AppHostMayBeRunningAsync(cancellationToken);
        var start = await Aspire(["start", "--apphost", project.AppHostDirectory, "--non-interactive", "--format", "Json"], progress, TimeSpan.FromMinutes(30), cancellationToken, startTimeout);
        var stop = AspireSpec(["stop", "--apphost", project.AppHostDirectory, "--non-interactive"], _ => { }, TimeSpan.FromMinutes(5));

        if (start.Succeeded is false)
        {
            if (runningBefore is false)
            {
                await Runner.RunAsync(stop with { OnOutputLine = progress }, CancellationToken.None);
            }

            return StepResult.FromProcess(start, "", failed, followUp) with { Status = StepStatus.Warning };
        }

        var keep = false;
        ProcessResult? stopped = null;

        try
        {
            foreach (var resource in AspireResourcesToWaitFor(project.AppHostDirectory))
            {
                if (await WaitUntilHealthyAsync(resource, containerProblem is null, progress, cancellationToken) is { } reason)
                    return StepResult.Warning($"Aspire started, {resource} didn't get healthy", reason, followUp);
            }

            if (keepRunning is false)
            {
                stopped = await Runner.RunAsync(stop with { OnOutputLine = progress }, CancellationToken.None);
                var result = StepResult.FromProcess(stopped, "Started and stopped the project with Aspire", "Started the project with Aspire, but couldn't stop it",
                    $"cd {ProcessSpec.Quote(project.Directory)} && aspire stop", "its images and builds are ready, so the IDE starts it fast");

                return stopped.Succeeded ? result : result with { Status = StepStatus.Warning };
            }

            Running = new RunningProject
            {
                DashboardUrl = DashboardUrlRegex().Match(start.Output) is { Success: true } dashboard ? dashboard.Groups["url"].Value : null,
                AppUrl = await AspireResourceUrlAsync("serverweb", cancellationToken) ?? ReadLaunchUrl(ServerWebDirectory),
                StopAsync = async detached =>
                {
                    if (detached is false || Runner.StartDetached(stop) is false)
                    {
                        await Runner.RunAsync(stop, CancellationToken.None);
                    }
                }
            };

            keep = true;

            return containerProblem is { } absent
                ? StepResult.Succeeded("Started the project with Aspire", "without its containers", $"{absent.Detail}, so Keycloak, Mailpit and the other containers aren't running.{(absent.Fix is null ? "" : $" {absent.Fix}")}")
                : StepResult.Succeeded("Started the project with Aspire");
        }
        finally
        {
            if (keep is false && stopped is null)
            {
                await Runner.RunAsync(stop with { OnOutputLine = progress }, CancellationToken.None);
            }
        }
    }

    public async Task<StepResult> DotnetRunAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        const string skipped = "Didn't start the project";
        string[] servers = project.IsApiStandalone ? [project.ServerApiDirectory, ServerWebDirectory] : [ServerWebDirectory];
        var followUp = string.Join(" & ", servers.Select(server => $"dotnet run --project {ProcessSpec.Quote(Path.GetRelativePath(project.Directory, server))}"));

        if (servers.FirstOrDefault(server => System.IO.Directory.Exists(server) is false || ReadLaunchUrl(server) is null) is { } missing)
            return StepResult.Skipped(skipped, $"{Path.GetFileName(missing)} has no URL in its launchSettings.json");

        var running = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var runs = new List<Task<ProcessResult?>>();

        async Task StopAsync()
        {
            await running.CancelAsync();
            await Task.WhenAny(Task.WhenAll(runs), Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None));
        }

        foreach (var server in servers)
        {
            var url = ReadLaunchUrl(server)!;
            var run = RunUntilStoppedAsync(new ProcessSpec
            {
                FileName = "dotnet",
                Arguments = ["run", "--project", server, .. migrationAdded ? Array.Empty<string>() : ["--no-build"], .. project.BuildProperties.Select(p => $"--property:{p}")],
                WorkingDirectory = project.Directory,
                Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1" }
            }, running.Token);

            runs.Add(run);

            if (await WaitUntilAliveAsync(new Uri(url), run, progress, cancellationToken) is { } reason)
            {
                await StopAsync();
                running.Dispose();
                return StepResult.Warning($"Couldn't start {Path.GetFileName(server)}", reason, $"cd {ProcessSpec.Quote(project.Directory)} && {followUp}");
            }
        }

        Running = new RunningProject { AppUrl = ReadLaunchUrl(ServerWebDirectory), StopAsync = _ => StopAsync(), Exited = Task.WhenAny(runs) };
        return StepResult.Succeeded("Started the project", Running.AppUrl);
    }

    private async Task<ProcessResult?> RunUntilStoppedAsync(ProcessSpec spec, CancellationToken cancellationToken)
    {
        try
        {
            return await Runner.RunAsync(spec, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private async Task<string?> WaitUntilAliveAsync(Uri url, Task<ProcessResult?> run, Action<string> progress, CancellationToken cancellationToken)
    {
        var alive = new Uri(url, "alive");
        var deadline = DateTime.UtcNow.AddMinutes(10);

        progress($"Waiting for {url}");

        while (DateTime.UtcNow < deadline)
        {
            if (run.IsCompleted)
                return await run is { } result ? DiagnosticCodes.FirstErrorLine(result.Output) ?? $"it exited with code {result.ExitCode}" : "it was stopped";

            if (await AliveProbe(alive, cancellationToken))
                return null;

            await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(2), cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
        }

        return "it didn't answer within 10 minutes";
    }

    private static async Task<bool> IsAliveAsync(Uri url, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exp) when (exp is HttpRequestException || (exp is TaskCanceledException && cancellationToken.IsCancellationRequested is false))
        {
            return false;
        }
    }

    public static string? ReadLaunchUrl(string projectDirectory)
    {
        var path = Path.Combine(projectDirectory, "Properties", "launchSettings.json");

        try
        {
            if (File.Exists(path) is false)
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            if (document.RootElement.TryGetProperty("profiles", out var profiles) is false || profiles.ValueKind is not JsonValueKind.Object)
                return null;

            var url = profiles.EnumerateObject()
                .Select(profile => profile.Value)
                .Where(profile => profile.ValueKind is JsonValueKind.Object && Text(profile, "commandName") is "Project")
                .Select(profile => Text(profile, "applicationUrl"))
                .FirstOrDefault(urls => string.IsNullOrWhiteSpace(urls) is false)?
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            return url is null ? null : WildcardHostRegex().Replace(url, "://localhost");
        }
        catch (Exception exp) when (exp is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task<string?> AspireResourceUrlAsync(string resource, CancellationToken cancellationToken)
    {
        var describe = await Aspire(["describe", resource, "--format", "json", "--apphost", project.AppHostDirectory, "--non-interactive", "--nologo"], _ => { }, TimeSpan.FromMinutes(1), cancellationToken);

        var urls = ReadJson(describe.Output, root => root.TryGetProperty("resources", out var all) && all.ValueKind is JsonValueKind.Array
            ? all.EnumerateArray()
                .Where(r => Text(r, "name") == resource || Text(r, "displayName") == resource)
                .SelectMany(r => r.TryGetProperty("urls", out var list) && list.ValueKind is JsonValueKind.Array ? list.EnumerateArray() : Enumerable.Empty<JsonElement>())
                .Where(u => u.TryGetProperty("isInternal", out var isInternal) is false || isInternal.ValueKind is not JsonValueKind.True)
                .Select(u => Text(u, "url"))
                .OfType<string>()
                .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                .ToList()
            : null) ?? [];

        return urls.FirstOrDefault(u => u.StartsWith("https:", StringComparison.OrdinalIgnoreCase)) ?? urls.FirstOrDefault();
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

    private async Task<string?> WaitUntilHealthyAsync(string resource, bool containersRun, Action<string> progress, CancellationToken cancellationToken)
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

            var resources = (await DescribeAspireResourcesAsync(cancellationToken)).Where(r => containersRun || r.Type is not "Container").ToList();
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

    private async Task<IReadOnlyList<(string Name, string? Type, string? State, string? HealthStatus, int? ExitCode)>> DescribeAspireResourcesAsync(CancellationToken cancellationToken)
    {
        var describe = await Aspire(["describe", "--format", "json", "--apphost", project.AppHostDirectory, "--non-interactive", "--nologo"], _ => { }, TimeSpan.FromMinutes(1), cancellationToken);

        return ReadJson(describe.Output, root => root.TryGetProperty("resources", out var all) && all.ValueKind is JsonValueKind.Array
            ? all.EnumerateArray().Select(r => (Name: Text(r, "displayName") ?? Text(r, "name") ?? "?", Type: Text(r, "resourceType"), State: Text(r, "state"), HealthStatus: Text(r, "healthStatus"), ExitCode: r.TryGetProperty("exitCode", out var code) && code.ValueKind is JsonValueKind.Number ? code.GetInt32() : (int?)null)).ToList()
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

    private Task<ProcessResult> Aspire(string[] arguments, Action<string> progress, TimeSpan timeout, CancellationToken cancellationToken, IReadOnlyDictionary<string, string?>? environment = null)
    {
        return Runner.RunAsync(AspireSpec(arguments, progress, timeout, environment), cancellationToken);
    }

    private async Task<bool> AppHostMayBeRunningAsync(CancellationToken cancellationToken)
    {
        var ps = await Runner.RunAsync(AspireSpec(["ps", "--format", "Json", "--non-interactive"], _ => { }, TimeSpan.FromMinutes(1)), cancellationToken);

        if (ps.Succeeded is false)
            return true;

        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(ps.Output) ? "[]" : ps.Output);
            return Strings(document.RootElement).Any(value => value.StartsWith(project.AppHostDirectory, StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return true;
        }

        static IEnumerable<string> Strings(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => [element.GetString()!],
            JsonValueKind.Array => element.EnumerateArray().SelectMany(Strings),
            JsonValueKind.Object => element.EnumerateObject().SelectMany(property => Strings(property.Value)),
            _ => []
        };
    }

    private ProcessSpec AspireSpec(string[] arguments, Action<string> progress, TimeSpan timeout, IReadOnlyDictionary<string, string?>? environment = null)
    {
        return new ProcessSpec { FileName = "aspire", Arguments = arguments, WorkingDirectory = project.Directory, Environment = environment, OnOutputLine = progress, Timeout = timeout };
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

        if (Runner.FindExecutable("apt-get") is null)
            return StepResult.Succeeded(title, detail, "Playwright installs the browsers' system libraries only with apt-get; if the UI tests can't start a browser, install them with your package manager.");

        var dependencies = install with { Arguments = [driver.Cli, "install-deps", .. browsers], OnOutputLine = progress };
        var followUp = "sudo " + dependencies.CommandLine;

        if (cli.Environment.IsElevated is false)
        {
            if (Runner.FindExecutable("sudo") is null || (await Runner.RunAsync(new ProcessSpec { FileName = "sudo", Arguments = ["-n", "true"], Timeout = TimeSpan.FromSeconds(10) }, cancellationToken)).Succeeded is false)
                return StepResult.Warning(title, "its system libraries need sudo", followUp);

            dependencies = dependencies.WithSudo(Runner);
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

        var installed = await ListExtensionsAsync(code, cancellationToken);
        var wanted = recommended.Where(id => installed?.Contains(id) is not true).ToList();

        if (installed is not null && wanted.Count == 0)
            return StepResult.Succeeded("Extensions already installed", $"{recommended.Count} recommended for VS Code");

        var missing = wanted;
        var builtIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ProcessResult? last = null;

        for (var attempt = 0; attempt < 2 && missing.Count > 0; attempt++)
        {
            last = await Runner.RunAsync(new ProcessSpec { FileName = code.Executable, Arguments = InstallArguments(missing), OnOutputLine = progress, Timeout = TimeSpan.FromMinutes(15) }, cancellationToken);
            builtIn.UnionWith(BuiltInExtensionRegex().Matches(last.Output).Select(m => m.Groups["id"].Value));

            var now = await ListExtensionsAsync(code, cancellationToken) ?? [];
            missing = [.. missing.Where(id => now.Contains(id) is false && builtIn.Contains(id) is false)];
        }

        var added = wanted.Where(id => builtIn.Contains(id) is false && missing.Contains(id) is false).ToList();

        if (missing.Count > 0)
            return StepResult.Warning($"Couldn't install {missing.Count} VS Code extension{(missing.Count == 1 ? "" : "s")}", ExtensionNames(missing),
                $"{ProcessSpec.Quote(code.Executable)} {string.Join(' ', InstallArguments(missing))}", last is null ? null : DiagnosticCodes.FirstErrorLine(last.Output));

        return added.Count == 0
            ? StepResult.Succeeded("Extensions already installed", $"{recommended.Count} recommended for VS Code")
            : StepResult.Succeeded($"Installed {added.Count} VS Code extension{(added.Count == 1 ? "" : "s")}", ExtensionNames(added));
    }

    private async Task<HashSet<string>?> ListExtensionsAsync(Ide code, CancellationToken cancellationToken)
    {
        var listed = await Runner.RunAsync(new ProcessSpec { FileName = code.Executable, Arguments = ["--list-extensions"], Timeout = TimeSpan.FromMinutes(2) }, cancellationToken);
        return listed.Succeeded ? listed.OutputLines.Select(l => l.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase) : null;
    }

    private static string[] InstallArguments(IEnumerable<string> extensions) => [.. extensions.SelectMany(id => new[] { "--install-extension", id })];

    private static string ExtensionNames(IReadOnlyList<string> extensions)
        => string.Join(", ", extensions.Take(3).Select(id => id[(id.IndexOf('.') + 1)..])) + (extensions.Count > 3 ? $" and {extensions.Count - 3} more" : "");

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
            return StepResult.Warning("Didn't open an IDE", $"{IdeLocator.Title(ide)} isn't installed");

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
        await Gh(["auth", "setup-git", "--hostname", "github.com", "--force"], cancellationToken);

        using var signIn = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var codeShown = 0;

        var login = Runner.RunAsync(new ProcessSpec
        {
            FileName = "gh",
            Arguments = ["auth", "login", "--web", "--clipboard", "--git-protocol", "https", "--hostname", "github.com"],
            WorkingDirectory = project.Directory,
            Timeout = TimeSpan.FromMinutes(15),
            OnOutputLine = line =>
            {
                if (OneTimeCodeRegex().Match(line) is not { Success: true } code || Interlocked.Exchange(ref codeShown, 1) is 1)
                    return;

                var copied = line.Contains("copied to clipboard", StringComparison.OrdinalIgnoreCase) ? " [grey](copied to the clipboard)[/]" : "";
                cli.Console.Out.MarkupLine($"    Enter [bold]{code.Value}[/]{copied} at {GitHubDevicePage}, which opens in your browser.");
                cli.Console.Out.MarkupLine("    [grey]Press Esc to skip signing in; bit goes on without the GitHub repo.[/]");
                ProjectRun.OpenInBrowser(cli, GitHubDevicePage);
            }
        }, signIn.Token);

        var skip = SkipRequested(signIn.Token);
        var skipped = await Task.WhenAny(login, skip) == skip && skip.IsCompletedSuccessfully;

        await signIn.CancelAsync();

        try
        {
            var result = await login;

            if (skipped is false)
                return result.Succeeded
                    ? StepResult.Succeeded("Signed in to GitHub")
                    : StepResult.Warning("Not signed in to GitHub", "the sign-in didn't finish", "gh auth login --web");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
        }

        return StepResult.Warning("Skipped signing in to GitHub", "you pressed Esc", "gh auth login --web");
    }

    private const string GitHubDevicePage = "https://github.com/login/device";

    public Func<CancellationToken, Task> SkipRequested { get; init; } = WaitForEscapeAsync;

    private static async Task WaitForEscapeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (Console.IsInputRedirected is false && Console.KeyAvailable && Console.ReadKey(intercept: true).Key is ConsoleKey.Escape)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
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
        return Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = arguments, WorkingDirectory = workingDirectory ?? project.Directory, Environment = GitWithoutAutoMaintenance, Timeout = TimeSpan.FromMinutes(5) }, cancellationToken);
    }

    private Task<ProcessResult> Dotnet(string[] arguments, CancellationToken cancellationToken, Action<string>? progress, TimeSpan timeout, AdministratorSession? administrator = null)
    {
        var spec = new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = project.Directory,
            OnOutputLine = progress,
            Timeout = timeout,
            Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1", ["MSBUILDTERMINALLOGGER"] = "off", ["DOTNET_CLI_UI_LANGUAGE"] = "en" }
        };

        return administrator is null ? Runner.RunAsync(spec, cancellationToken) : administrator.RunAsync(spec, cancellationToken);
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

    [GeneratedRegex(@"\b[A-Z0-9]{4}-[A-Z0-9]{4}\b")]
    private static partial Regex OneTimeCodeRegex();

    [GeneratedRegex(@"""dashboardUrl""\s*:\s*""(?<url>[^""]+)""")]
    private static partial Regex DashboardUrlRegex();

    [GeneratedRegex(@"://(\*|\+|0\.0\.0\.0|\[::\])")]
    private static partial Regex WildcardHostRegex();
}
