using System.Diagnostics;
using Bit.Cli.Commands;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Bit.Cli.Templates;
using Bit.Cli.Tools;
using Spectre.Console;

namespace Bit.Cli.Projects;

public sealed class NewWorkflow(CliServices cli)
{
    private const string CreateProjectPage = "https://bitplatform.dev/templates/create-project";

    public async Task<int> RunAsync(NewRequest request, CancellationToken cancellationToken)
    {
        if (request.Yes || request.NonInteractive)
        {
            cli.DisablePrompts();
        }

        var interactive = cli.IsInteractive && cli.Prompter.CanPrompt;

        WriteHeader(cli);

        var manifest = (request.TemplatePackage is not null ? TemplateSource.ReadManifest(Path.GetFullPath(request.TemplatePackage, cli.Environment.CurrentDirectory)) : null) ?? TemplateManifest.Embedded;
        var selection = new TemplateSelection(manifest);

        foreach (var (option, value) in request.TemplateValues)
        {
            if (selection.Set(option, value) is { } error)
                return UsageError(error);
        }

        var name = request.Name;

        if (name is null)
            return UsageError($"Pass the project name: bit new <name>. {CreateProjectPage} builds the whole command.");

        if (ProjectName.Validate(name) is { } nameError)
            return UsageError($"{nameError} Try: bit new {ProjectName.Suggest(name)}");

        if (request.GitHubRepo && string.Equals(selection["pipeline"], "GitHub", StringComparison.OrdinalIgnoreCase) is false)
            return UsageError("--github-repo needs the GitHub pipeline (--pipeline GitHub).");

        if (request.GitHubRepo && request.NoGit)
            return UsageError("--github-repo pushes the project's git repository, so it can't go with --no-git.");

        var directory = Path.GetFullPath(request.Output ?? name, cli.Environment.CurrentDirectory);

        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            return UsageError($"{directory} already exists and isn't empty. Pick another name, or a folder with -o.");

        var (platforms, platformError) = SharedOptions.ParsePlatforms(request.Platforms, cli.Environment.Os);

        if (platformError is not null)
            return UsageError(platformError);

        var ide = request.NoOpen ? IdeLocator.None : request.Ide;
        var needs = new ToolNeeds { Aspire = selection.Aspire, Containers = selection.AspireContainers(), NativeWebAssembly = selection.IsTrue("offlineDb"), GitHubRepo = request.GitHubRepo, Platforms = platforms, Ide = ide, MinimumSdk = MinimumSdk(request) };
        var hardware = request.NoSetup ? null : ProbeHardwareAsync(cli, directory, cancellationToken);
        var selectedTools = request.NoTools ? [] : await ChooseToolsAsync(cli, needs, request.Tools, request.ToolsGiven, interactive, hardware, cancellationToken);

        if (request.NoTools && hardware is not null)
        {
            WriteHardwareWarnings(cli, await hardware, needs);
        }

        ide ??= DefaultIde(selectedTools);

        if (interactive)
        {
            AskUsageTelemetry();
        }

        var context = new ProjectContext { Name = name, Directory = directory, Platforms = platforms, Template = selection };

        TagTelemetry(selection, platforms, selectedTools, ide, request);
        WritePlan(context, selection, selectedTools, ide, request);

        if (request.DryRun)
        {
            cli.Console.Out.MarkupLine("[grey]Dry run: nothing was changed.[/]");
            return CliApp.ExitOk;
        }

        if (interactive && cli.Prompter.Confirm("Create it?", true) is false)
            return CliApp.ExitOk;

        cli.Console.Out.WriteLine();

        var stopwatch = Stopwatch.StartNew();
        var steps = new StepRunner(cli);
        var projectSteps = new ProjectSteps(cli, context);

        if (selectedTools.Count > 0)
        {
            await new ToolInstaller(cli, steps).InstallAsync(selectedTools, CreateToolContext(cli, needs), cancellationToken);
        }

        await CreateAsync(steps, context, selection, request, cancellationToken);

        if (request.NoCertificate is false)
        {
            await RunProjectStepAsync(steps, context, "certificate", "Creating a unique app certificate", projectSteps.AppCertificateAsync, cancellationToken);
        }

        if (request.NoGit is false)
        {
            await RunProjectStepAsync(steps, context, "git", "Initializing git", projectSteps.GitAsync, cancellationToken);
        }

        await RunSetupStepsAsync(cli, steps, context, projectSteps, request.NoWorkloads, request.NoRestore, request.NoBuild, cancellationToken);

        if (request.NoFormat is false)
        {
            await RunProjectStepAsync(steps, context, "format", "Formatting the code", projectSteps.FormatAsync, cancellationToken);
        }

        if (request.NoMigration is false)
        {
            await RunProjectStepAsync(steps, context, "migration", "Adding the Initial EF Core migration", projectSteps.MigrationAsync, cancellationToken);
        }

        if (request.GitHubRepo)
        {
            if (context.Exists && context.GitReady && await projectSteps.CanSignInToGitHubAsync(cancellationToken))
            {
                await steps.RunAsync("github-login", "Signing in to GitHub in your browser", projectSteps.GitHubSignInAsync, cancellationToken, needsTerminal: true);
            }

            await RunProjectStepAsync(steps, context, "github-repo", "Creating a private GitHub repo", projectSteps.GitHubRepositoryAsync, cancellationToken);
        }

        if (request.NoTrust is false)
        {
            await RunProjectStepAsync(steps, context, "trust", "Trusting the folder", projectSteps.TrustAsync, cancellationToken);
        }

        if (ide is IdeLocator.VsCode)
        {
            await RunProjectStepAsync(steps, context, "vscode-extensions", "Installing VS Code extensions", projectSteps.VsCodeExtensionsAsync, cancellationToken);
        }

        if (ide is not IdeLocator.None)
        {
            await RunProjectStepAsync(steps, context, "open", "Opening the IDE", (progress, ct) => projectSteps.OpenIdeAsync(ide, progress, ct), cancellationToken);
        }

        WriteSummary(cli, steps, context, selection, stopwatch.Elapsed, request.NoSetup);
        return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
    }

    public static async Task RunSetupStepsAsync(CliServices cli, StepRunner steps, ProjectContext context, ProjectSteps projectSteps, bool noWorkloads, bool noRestore, bool noBuild, CancellationToken cancellationToken)
    {
        if (noWorkloads is false)
        {
            await RunProjectStepAsync(steps, context, "workloads", "Installing build tools", projectSteps.WorkloadsAsync, cancellationToken);

            if (context.Platforms.Contains(Platform.Android))
            {
                await RunProjectStepAsync(steps, context, "android", "Installing the Android SDK and Java", projectSteps.AndroidDependenciesAsync, cancellationToken);
            }
        }

        if (noRestore is false)
        {
            await RunProjectStepAsync(steps, context, "restore", "Restoring NuGet packages", projectSteps.RestoreAsync, cancellationToken);
        }

        if (noBuild is false)
        {
            foreach (var target in Platforms.BuildTargets(context.Name, context.Platforms, context.TargetFrameworkVersion))
            {
                await RunProjectStepAsync(steps, context, $"build-{Platforms.Name(target.Platform)}", $"Building the {Platforms.Title(target.Platform)} app", (progress, ct) => projectSteps.BuildAsync(target, progress, ct), cancellationToken);
            }

            await RunProjectStepAsync(steps, context, "playwright", "Installing Chromium for UI tests", projectSteps.PlaywrightAsync, cancellationToken);
        }
    }

    public static async Task RunProjectStepAsync(StepRunner steps, ProjectContext context, string id, string title, Func<Action<string>, CancellationToken, Task<StepResult>> work, CancellationToken cancellationToken)
    {
        if (context.Exists is false)
        {
            steps.Add(id, StepResult.Skipped(title, "the project wasn't created"));
            return;
        }

        await steps.RunAsync(id, title, work, cancellationToken);
    }

    public static ToolContext CreateToolContext(CliServices cli, ToolNeeds needs)
    {
        return new ToolContext(cli.Environment, cli.Runner, needs, PackageManagers.Detect(cli.Environment, cli.Runner));
    }

    public static Task<HardwareFacts>? ProbeHardwareAsync(CliServices cli, string directory, CancellationToken cancellationToken)
    {
        if (cli.Environment.IsCI)
            return null;

        var existing = new DirectoryInfo(directory);

        while (existing is { Exists: false } && existing.Parent is not null)
        {
            existing = existing.Parent;
        }

        return Hardware.ProbeAsync(cli.Environment, cli.Runner, existing.FullName, cancellationToken);
    }

    public static void WriteHardwareWarnings(CliServices cli, HardwareFacts facts, ToolNeeds needs)
    {
        var warnings = Hardware.Evaluate(facts, needs, cli.Environment.Os);
        cli.Log.Write($"Hardware: {Hardware.Describe(facts)}");
        cli.Telemetry.SetTag(TelemetryFields.Hardware, string.Join(',', warnings.Select(w => w.Id)));

        foreach (var warning in warnings)
        {
            cli.Console.StepWarning(warning.Text, warning.Link is null ? null : $"How to turn it on: {warning.Link}");
        }
    }

    public static async Task<IReadOnlyList<ToolCheck>> ChooseToolsAsync(CliServices cli, ToolNeeds needs, IReadOnlyList<string> requested, bool requestedGiven, bool interactive, Task<HardwareFacts>? hardware, CancellationToken cancellationToken)
    {
        var context = CreateToolContext(cli, needs);
        var (checks, facts) = await cli.Console.RunWithStatusAsync("Checking this machine", async _ => (await ToolCatalog.CheckAsync(context, cancellationToken), hardware is null ? null : await hardware));

        if (facts is not null)
        {
            WriteHardwareWarnings(cli, facts, needs);
        }

        var missing = checks.Where(c => c.Status.IsSatisfied is false).ToList();

        cli.Log.Write("Tools: " + string.Join(", ", checks.Select(c => $"{c.Tool.Id}={c.Status.State}{(c.Status.Version is null ? "" : " " + c.Status.Version)}")));

        if (missing.Count == 0)
        {
            cli.Console.Step(StepStatus.Succeeded, "This machine has every tool the project needs");
            return [];
        }

        if (requestedGiven)
        {
            if (requested.Any(r => r.Equals("none", StringComparison.OrdinalIgnoreCase)))
                return [];

            foreach (var unknown in requested.Where(r => ToolCatalog.Find(r) is null))
            {
                cli.Console.Warn($"There's no tool called '{unknown}'. Known tools: {string.Join(", ", ToolCatalog.All.Select(t => t.Id))}.");
            }

            return [.. missing.Where(c => requested.Contains(c.Tool.Id, StringComparer.OrdinalIgnoreCase))];
        }

        if (interactive is false)
            return [.. missing.Where(c => c.Needed)];

        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine("[bold]Missing on this machine[/] [grey](checked ones are needed by this project)[/]");

        return cli.Prompter.MultiSelect("Install these?", missing, missing.Where(c => c.Needed), c => DescribeTool(c));
    }

    public static string DescribeTool(ToolCheck check)
    {
        var state = check.Status.State switch
        {
            ToolState.Outdated => $" (found {check.Status.Version}, {check.Status.Detail})",
            ToolState.NotRunning => " (installed, not running)",
            _ => ""
        };

        var how = check.Action switch
        {
            null => ", you install it",
            { Elevation: Elevation.Admin } => ", needs admin",
            { Elevation: Elevation.Sudo } => ", needs sudo",
            _ => ""
        };

        return $"{check.Action?.Title ?? check.Tool.Name}{state}: {check.Why}{how}";
    }

    public static void WriteHeader(CliServices cli)
    {
        var provenance = BuildInfo.RunUrl is null
            ? $"local build{(BuildInfo.ShortCommit is null ? "" : $" of {BuildInfo.ShortCommit}")}"
            : $"built by GitHub Actions from {BuildInfo.ShortCommit}";

        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine($"  [bold blue]bit[/] [grey]CLI {Markup.Escape(BuildInfo.Version)}[/]");
        cli.Console.Out.MarkupLine($"  [grey]open source at github.com/bitfoundation/bitplatform · {Markup.Escape(provenance)} · details: bit about[/]");
        cli.Console.Out.WriteLine();
    }

    private Version? MinimumSdk(NewRequest request)
    {
        string? globalJson = null;

        if (request.TemplatePackage is not null)
        {
            globalJson = TemplateSource.ReadEntry(Path.GetFullPath(request.TemplatePackage, cli.Environment.CurrentDirectory), "Bit.Boilerplate/global.json");
        }

        if (globalJson is null)
        {
            var cachedHive = Path.Combine(cli.Environment.BitDirectory, "templates", request.TemplateVersion ?? BuildInfo.Version, "packages");

            if (Directory.Exists(cachedHive) && Directory.EnumerateFiles(cachedHive, "*.nupkg").FirstOrDefault() is { } cached)
            {
                globalJson = TemplateSource.ReadEntry(cached, "Bit.Boilerplate/global.json");
            }
        }

        if (globalJson is null)
            return null;

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(globalJson);
            return document.RootElement.TryGetProperty("sdk", out var sdk) && sdk.TryGetProperty("version", out var version) && Version.TryParse(version.GetString(), out var parsed)
                ? parsed
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public static HashSet<Platform> AskPlatforms(CliServices cli, HashSet<Platform> current)
    {
        var native = Platforms.AvailableOn(cli.Environment.Os).Where(p => p is not Platform.Web).ToList();

        if (native.Count == 0)
            return current;

        cli.Console.Out.MarkupLine($"[grey]Every project has the web, Android, iOS, Windows and macOS apps. Pick only the native apps to set up on this machine now: each needs several GB of extra build tools and minutes of build, so fewer is faster. Add one any time later from the project folder, e.g.[/] bit setup --platforms {Platforms.Name(native[0])}");

        var chosen = cli.Prompter.MultiSelect("Set up native apps on this machine now too?", native, native.Where(current.Contains), Platforms.Title);
        return [Platform.Web, .. chosen];
    }

    private string DefaultIde(IReadOnlyList<ToolCheck> selectedTools)
    {
        if (cli.Environment.IsCI)
            return IdeLocator.None;

        return IdeLocator.FindVsCode(cli.Environment, cli.Runner) is not null || selectedTools.Any(t => t.Tool.Id is "vscode")
            ? IdeLocator.VsCode
            : IdeLocator.None;
    }

    private void AskUsageTelemetry()
    {
        var decision = TelemetryDecision.Resolve(cli.Environment, cli.Settings, BuildInfo.TelemetryConnectionString);

        if (decision.IsDefault is false || decision.CanSend is false || cli.Settings.UsageQuestionAsked)
            return;

        var share = cli.Prompter.Confirm("Also share which options and platforms you pick, anonymously? It helps decide what to make the default", false);
        cli.Settings.UsageQuestionAsked = true;

        if (share)
        {
            cli.Settings.Telemetry = TelemetryDecision.Name(TelemetryLevel.All);
        }

        cli.Settings.Save();
    }

    private async Task CreateAsync(StepRunner steps, ProjectContext context, TemplateSelection selection, NewRequest request, CancellationToken cancellationToken)
    {
        var source = new TemplateSource(cli.Environment, cli.Runner);
        TemplatePackage? package = null;

        await steps.RunAsync("template", "Getting bit Boilerplate", async (progress, ct) =>
        {
            var (installed, result) = await source.EnsureInstalledAsync(request.TemplateVersion, request.TemplatePackage, progress, ct);
            package = installed;
            cli.Telemetry.SetTag(TelemetryFields.TemplateVersion, installed?.Version);

            return installed is null
                ? StepResult.FromProcess(result.Succeeded ? result with { ExitCode = 1 } : result, "", "Couldn't get bit Boilerplate", request.TemplatePackage is null ? $"dotnet new install Bit.Boilerplate::{request.TemplateVersion ?? BuildInfo.Version}" : null)
                : StepResult.Succeeded($"Got bit Boilerplate {installed.Version}", request.TemplatePackage is null ? "from nuget.org" : "from the local package");
        }, cancellationToken);

        if (package is null)
        {
            steps.Add("create", StepResult.Skipped($"Didn't create {context.Name}", "the template isn't available"));
            return;
        }

        await steps.RunAsync("create", $"Creating {context.Name}", async (progress, ct) =>
        {
            var manifest = TemplateSource.ReadManifest(package.PackagePath) ?? selection.Manifest;
            var arguments = selection.ToTemplateArguments().Concat(request.ExtraTemplateArguments).ToList();
            var result = await source.CreateAsync(package, manifest, context.Name, context.Directory, arguments, progress, ct);

            if (result.Succeeded && context.Exists is false)
                return StepResult.Failed($"Couldn't create {context.Name}", "dotnet new finished without creating the solution", hint: DiagnosticCodes.FirstErrorLine(result.Output));

            var files = context.Exists ? Directory.EnumerateFiles(context.Directory, "*", SearchOption.AllDirectories).Count() : 0;
            return StepResult.FromProcess(result, $"Created {context.Name}", $"Couldn't create {context.Name}", null, $"bit Boilerplate {package.Version}, {files} files");
        }, cancellationToken);
    }

    private void TagTelemetry(TemplateSelection selection, IReadOnlySet<Platform> platforms, IReadOnlyList<ToolCheck> tools, string ide, NewRequest request)
    {
        foreach (var parameter in selection.Manifest.Parameters.Where(p => p.Type is not TemplateParameterType.String))
        {
            cli.Telemetry.SetTag(TelemetryFields.TemplatePrefix + parameter.Name, selection[parameter.Name]);
        }

        cli.Telemetry.SetTag(TelemetryFields.Platforms, string.Join(',', platforms.Order().Select(Platforms.Name)));
        cli.Telemetry.SetTag(TelemetryFields.Tools, string.Join(',', tools.Select(t => t.Tool.Id)));
        cli.Telemetry.SetTag(TelemetryFields.Ide, ide);
        cli.Telemetry.SetTag(TelemetryFields.TemplateVersion, request.TemplatePackage is null ? request.TemplateVersion ?? BuildInfo.Version : "local");
    }

    private void WritePlan(ProjectContext context, TemplateSelection selection, IReadOnlyList<ToolCheck> tools, string ide, NewRequest request)
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap().PadRight(2)).AddColumn();

        void Row(string name, string value) => grid.AddRow($"[grey]{Markup.Escape(name)}[/]", Markup.Escape(value));

        var options = selection.NonDefaultValues().Select(v => v.Parameter.Type is TemplateParameterType.Bool
            ? (v.Value is "true" ? TemplateLabels.For(v.Parameter) : $"no {TemplateLabels.For(v.Parameter)}")
            : $"{TemplateLabels.For(v.Parameter)}: {v.Value}").ToList();

        Row("Project", $"{context.Name} in {context.Directory}");
        Row("Options", options.Count == 0 ? "the template's defaults" : string.Join(" · ", options));
        Row("Platforms", string.Join(", ", context.Platforms.Order().Select(Platforms.Title)));

        if (tools.Count > 0)
        {
            Row("Install", string.Join(", ", tools.Select(t => t.Tool.Name)));
        }

        var stepsList = new List<string> { "create" };
        if (request.NoCertificate is false) stepsList.Add("unique app certificate");
        if (request.NoGit is false) stepsList.Add("git with develop and main");
        if (request.NoWorkloads is false) stepsList.Add("build tools");
        if (request.NoRestore is false) stepsList.Add("restore");
        if (request.NoBuild is false) stepsList.Add("build");
        if (request.NoBuild is false && cli.Environment.IsCI is false) stepsList.Add("Chromium for UI tests");
        if (request.NoFormat is false) stepsList.Add("dotnet format");
        if (request.NoMigration is false && selection.Database is not "Other") stepsList.Add("initial migration");
        if (request.GitHubRepo) stepsList.Add("private GitHub repository");
        if (request.NoTrust is false) stepsList.Add("trust for VS Code and AI tools");
        if (ide is IdeLocator.VsCode) stepsList.Add("VS Code extensions");
        if (ide is not IdeLocator.None) stepsList.Add($"open in {IdeLocator.Title(ide)}");
        Row("Then", string.Join(", ", stepsList));

        cli.Console.Out.WriteLine();
        cli.Console.Out.Write(new Panel(grid).Header("[bold]bit new[/]").Border(BoxBorder.Rounded).BorderStyle(new Style(Color.Grey)).Padding(1, 0, 1, 0));

        var adminTools = tools.Where(t => t.Action?.Elevation is Elevation.Admin).Select(t => t.Tool.Name).ToList();

        if (adminTools.Count > 0 && cli.Environment.IsElevated is false)
        {
            cli.Console.Out.MarkupLine($"[yellow]{cli.Console.WarnSymbol}[/] Windows will ask once for administrator permission (UAC), for: {Markup.Escape(string.Join(", ", adminTools))}.");
        }

        if (tools.Any(t => t.Action?.Elevation is Elevation.Sudo) && cli.Environment.IsElevated is false)
        {
            cli.Console.Out.MarkupLine($"[yellow]{cli.Console.WarnSymbol}[/] Some installs run with sudo; you'll be asked for your password once.");
        }

        if (cli.Environment.IsWindows && request.NoWorkloads is false)
        {
            cli.Console.Out.MarkupLine("[grey]Installing the build tools can show a Windows permission prompt too.[/]");
        }

        cli.Console.Out.WriteLine();
    }

    private static void WriteSummary(CliServices cli, StepRunner steps, ProjectContext context, TemplateSelection selection, TimeSpan elapsed, bool setupSkipped)
    {
        var failed = steps.Reports.Count(r => r.Result.Status is StepStatus.Failed);
        var warnings = steps.Reports.Count(r => r.Result.Status is StepStatus.Warning);

        cli.Console.Out.WriteLine();

        if (context.Exists is false)
        {
            cli.Console.Out.MarkupLine($"  [red bold]{Markup.Escape(context.Name)} wasn't created[/]");
        }
        else
        {
            var attention = failed + warnings == 0 ? "" : $", {failed + warnings} step{(failed + warnings == 1 ? "" : "s")} need{(failed + warnings == 1 ? "s" : "")} a look";
            cli.Console.Out.MarkupLine($"  [green bold]{Markup.Escape(context.Name)} is ready[/][grey]{Markup.Escape(attention)} in {CliConsole.FormatDuration(elapsed)}[/]");
            cli.Console.Out.WriteLine();

            var relative = Path.GetRelativePath(cli.Environment.CurrentDirectory, context.Directory);
            var run = selection.Aspire ? "aspire start" : $"dotnet watch --project src/Server/{context.Name}.Server.Web";

            if (setupSkipped)
            {
                cli.Console.Out.MarkupLine($"  [bold]Set it up[/]  cd {Markup.Escape(ProcessSpec.Quote(relative))}  [grey]then[/]  bit setup");
            }

            cli.Console.Out.MarkupLine($"  [bold]Run it[/]     cd {Markup.Escape(ProcessSpec.Quote(relative))}  [grey]then[/]  {Markup.Escape(run)}");
            cli.Console.Out.MarkupLine("  [bold]Sign in[/]    test@bitplatform.dev / 123456  [grey]seeded accounts, remove them before you deploy[/]");
            cli.Console.Out.MarkupLine("  [bold]Docs[/]       https://bitplatform.dev/templates");
        }

        var followUps = steps.Reports.Where(r => r.Result.FollowUp is not null && r.Result.Status is StepStatus.Failed or StepStatus.Warning).ToList();

        if (followUps.Count > 0)
        {
            cli.Console.Out.WriteLine();
            cli.Console.Out.MarkupLine("  [bold]To finish[/]");

            foreach (var report in followUps)
            {
                cli.Console.Out.MarkupLine($"    {Markup.Escape(report.Result.FollowUp!)}");
            }
        }

        if (failed > 0 || warnings > 0)
        {
            cli.Console.Out.WriteLine();

            if (cli.Log.FilePath is not null)
            {
                cli.Console.Out.MarkupLine($"  [grey]Log[/]        {Markup.Escape(cli.Log.FilePath)}");
            }

            if (failed > 0 && cli.Telemetry.ErrorId is { } errorId)
            {
                cli.Console.Out.MarkupLine($"  [grey]Error ID[/]   {errorId}  [grey]quote it in an issue to find this run[/]");
            }
        }

        cli.Console.Out.WriteLine();
    }

    private int UsageError(string message)
    {
        cli.Console.Fail(message);
        return CliApp.ExitUsage;
    }
}
