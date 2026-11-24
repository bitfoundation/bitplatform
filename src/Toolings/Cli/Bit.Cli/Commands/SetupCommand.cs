using System.CommandLine;
using System.Diagnostics;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Bit.Cli.Tools;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class SetupCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var shared = new SharedOptions();
        var pathArgument = new Argument<string?>("folder-or-repository") { Description = "The project's folder, or its repository's address to clone it first, e.g. https://github.com/contoso/Contoso. Default: the current folder.", Arity = ArgumentArity.ZeroOrOne };
        var ideOption = new Option<string?>("--ide") { Description = "Open the project in this IDE when done: code, vs, rider or none. Default: VS Code, when it's installed.", HelpName = "code|vs|rider|none" };
        ideOption.AcceptOnlyFromAmong(IdeLocator.VsCode, IdeLocator.VisualStudio, IdeLocator.Rider, IdeLocator.None);
        var noTrust = new Option<bool>("--no-trust") { Description = "Don't mark the folder as trusted for VS Code and the AI coding tools." };
        var noOpen = new Option<bool>("--no-open") { Description = "Don't open an IDE." };

        var command = new Command("setup", "Get an existing bit Boilerplate project ready on this machine: clone it when given its repository's address, then tools, workloads, packages, build, trust and IDE. Also adds an app later, e.g. bit setup --platforms android.")
        {
            pathArgument,
            ideOption,
            noTrust,
            noOpen
        };

        shared.AddTo(command);

        var projectOptions = new ProjectOptions();
        projectOptions.AddTo(command, projectOptions.ServerApi, projectOptions.ServerWeb, projectOptions.AppHost, projectOptions.ClientCore, projectOptions.Web, projectOptions.Maui, projectOptions.Windows, projectOptions.Workbench, projectOptions.LandingPage);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();

            if (parseResult.GetValue(shared.Yes) || parseResult.GetValue(shared.NonInteractive))
            {
                cli.DisablePrompts();
            }

            var interactive = cli.IsInteractive && cli.Prompter.CanPrompt;
            var argument = parseResult.GetValue(pathArgument);
            RepositoryAddress? repository = null;

            // A folder that exists stays a folder, even one whose name looks like git@host:path, as a Linux folder's can
            if (RepositoryAddress.LooksLikeOne(argument) && Directory.Exists(Path.GetFullPath(argument!, cli.Environment.CurrentDirectory)) is false)
            {
                repository = RepositoryAddress.Parse(argument);

                if (repository is null)
                    return cli.UsageError("repository-invalid", $"{argument} isn't a repository address bit can clone. Use https://host/owner/name or git@host:owner/name, with only letters, digits and . - _ ~ in its parts.");
            }

            var directory = Path.GetFullPath(argument is null || repository is not null ? "." : argument, cli.Environment.CurrentDirectory);
            var cloned = false;

            if (repository is not null)
            {
                var (target, relocated) = ProjectLocation.Choose(cli.Environment, Path.Combine(directory, repository.Name), repository.Name);
                directory = target;

                if (relocated is not null)
                {
                    cli.Console.Warn($"{relocated}, so bit clones {repository.Name} into {directory}.");
                    cli.Log.Write($"Project folder: {relocated}, using {directory}");
                }

                if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    if (cli.Runner.FindExecutable("git") is null)
                        return cli.UsageError("clone-check-needs-git", $"{directory} already exists, and bit needs git to tell whether it's a clone of {repository.Url}. Install git from https://git-scm.com/downloads and run this again, or move the folder away.");

                    if (await IsCloneOfAsync(cli, directory, repository, cancellationToken) is false)
                        return cli.UsageError("folder-not-empty", $"{directory} already exists and isn't a clone of {repository.Url}. Move it away, or run bit setup from another folder.");

                    cloned = true;
                }
            }

            if ((repository is null || cloned) && await cli.SelfUpdate.HandOffToProjectBitAsync(directory, cli.Arguments, cancellationToken) is { } handedOff)
                return handedOff;

            var name = repository is null && Directory.Exists(directory) ? ProjectContext.FindProjectName(directory) : null;

            if (repository is null && name is null)
            {
                return cli.UsageError("not-a-project", $"{directory} isn't a bit Boilerplate project folder (no *.Web.slnf or *.slnx). Run bit setup in the project's folder, or give it the project's repository to clone it first: bit setup https://github.com/contoso/Contoso");
            }

            var (platforms, error) = SharedOptions.ParsePlatforms(parseResult.GetValue(shared.Platforms), cli.Environment.Os);
            var (properties, propertyError) = SharedOptions.ParseProperties(parseResult.GetValue(shared.Properties));

            if ((error ?? propertyError) is { } usageError)
            {
                return cli.UsageError(error is null ? "properties-invalid" : "platforms-invalid", usageError);
            }

            var (parts, partsError) = repository is null ? projectOptions.Read(parseResult, cli, directory) : (ProjectParts.None, null);

            if (partsError is not null)
            {
                return cli.UsageError("project-part-missing", partsError);
            }

            NewWorkflow.WriteHeader(cli);

            var askedPlatforms = false;

            if (interactive && parseResult.GetResult(shared.Platforms) is null)
            {
                platforms = NewWorkflow.AskPlatforms(cli, platforms);
                askedPlatforms = true;
            }

            var stopwatch = Stopwatch.StartNew();
            var steps = new StepRunner(cli);

            if (repository is not null)
            {
                if (cloned)
                {
                    steps.Add("clone", StepResult.Skipped($"{repository.Name} is already cloned", ProjectLocation.ForDisplay(cli.Environment.CurrentDirectory, directory)));
                }
                else
                {
                    var cloneContext = new ProjectContext { Name = repository.Name, Directory = directory, Platforms = platforms };

                    if (parseResult.GetValue(shared.NoTools) is false)
                    {
                        await InstallCloneToolsAsync(cli, steps, repository, SharedOptions.SplitList(parseResult.GetValue(shared.Tools)), parseResult.GetResult(shared.Tools) is not null, cancellationToken);
                    }

                    var cloneSteps = new ProjectSteps(cli, cloneContext);

                    // The terminal stays the clone's: git may ask for a sign-in, and on github.com the GitHub CLI shows its code
                    var clone = await steps.RunAsync("clone", $"Cloning {repository.Url}", (progress, ct) => cloneSteps.CloneAsync(repository, progress, ct), cancellationToken, needsTerminal: cli.HasTerminal);

                    if (clone.Status is StepStatus.Failed)
                        return WriteSummary(cli, steps, repository.Name, stopwatch.Elapsed);

                    if (await cli.SelfUpdate.HandOffToProjectBitAsync(directory, ForTheClone(cli.Arguments, argument!, directory, askedPlatforms ? platforms : null), cancellationToken) is { } handedOffAfterClone)
                        return handedOffAfterClone;
                }

                name = ProjectContext.FindProjectName(directory);

                if (name is null)
                {
                    return cli.UsageError("not-a-project", $"{repository.Url} is cloned in {directory}, but it isn't a bit Boilerplate project (no *.Web.slnf or *.slnx at its root). If git couldn't check its files out, finish that first: cd {ProcessSpec.Quote(directory)} && git restore --source=HEAD :/ && bit setup");
                }

                (parts, partsError) = projectOptions.Read(parseResult, cli, directory);

                if (partsError is not null)
                {
                    return cli.UsageError("project-part-missing", partsError);
                }
            }

            var context = new ProjectContext { Name = name!, Directory = directory, Platforms = platforms, BuildProperties = properties, Parts = parts };

            if (Platforms.BuildsSolution(platforms) && context.AppId is { } appId && ProjectName.IsValidAppId(appId) is false)
            {
                return cli.UsageError("app-id-invalid", $"{context.MauiProject} gives the native apps the app id {appId}, and each part of an app id must start with a Latin letter and have only Latin letters, digits and underscores. Set a valid <ApplicationId> there, or leave the native apps out of --platforms.");
            }

            var aspire = Directory.Exists(context.AppHostDirectory);
            var requirements = TemplateRequirements.FromProject(context);
            var needs = new ToolNeeds { Aspire = aspire, ContainersRequired = aspire && context.NeedsContainers, NativeWebAssembly = context.UsesNativeWebAssembly, Platforms = platforms, Ide = IdeLocator.None, Sdk = requirements.Sdk, NodeMajor = requirements.NodeMajor, AspireVersion = requirements.Aspire };

            foreach (var note in context.PartsNotFound(settingUp: true))
            {
                cli.Console.Warn(note);
            }

            var hardware = NewWorkflow.ProbeHardwareAsync(cli, directory, cancellationToken);
            var tools = parseResult.GetValue(shared.NoTools)
                ? []
                : await NewWorkflow.ChooseToolsAsync(cli, needs, SharedOptions.SplitList(parseResult.GetValue(shared.Tools)), parseResult.GetResult(shared.Tools) is not null, interactive, hardware, cancellationToken);

            if (parseResult.GetValue(shared.NoTools) && hardware is not null)
            {
                NewWorkflow.WriteHardwareWarnings(cli, await hardware, needs);
            }

            var ide = parseResult.GetValue(noOpen) ? IdeLocator.None : parseResult.GetValue(ideOption) ?? DefaultIde(cli, tools);

            cli.Telemetry.SetTag(Telemetry.TelemetryFields.Platforms, string.Join(',', platforms.Order().Select(Platforms.Name)));
            cli.Telemetry.SetTag(Telemetry.TelemetryFields.Ide, ide);

            var administrator = await AdministratorNeeds.FindAsync(cli, tools, needs, parseResult.GetValue(shared.NoWorkloads) is false, cancellationToken);
            NewWorkflow.WriteAdministratorNote(cli, tools, administrator);
            cli.Console.Out.WriteLine();
            NewWorkflow.ShowAgentIdeas(cli);

            var projectSteps = new ProjectSteps(cli, context);
            var runMode = ProjectRun.Mode(cli, aspire, parseResult.GetValue(shared.NoBuild), parseResult.GetValue(shared.NoRun));

            await AdministratorNeeds.StartAsync(cli, administrator, cancellationToken);

            try
            {
                try
                {
                    if (tools.Count > 0)
                    {
                        await new ToolInstaller(cli, steps).InstallAsync(tools, NewWorkflow.CreateToolContext(cli, needs), cancellationToken);
                    }

                    var sdkReady = await NewWorkflow.RunSetupStepsAsync(cli, steps, context, projectSteps, parseResult.GetValue(shared.NoWorkloads), parseResult.GetValue(shared.NoRestore), parseResult.GetValue(shared.NoBuild), parseResult.GetValue(shared.NoBrowsers), runMode, cancellationToken);

                    if (sdkReady && parseResult.GetValue(shared.NoRestore) is false && projectSteps.LacksInitialMigration)
                    {
                        await NewWorkflow.RunProjectStepAsync(steps, context, "migration", "Adding the Initial EF Core migration", projectSteps.MigrationAsync, cancellationToken);
                    }

                    // As bit new does, so a clone on a teammate's computer opens without the prompts a new project skips; CI has no one to prompt
                    if (parseResult.GetValue(noTrust) is false && cli.Environment.IsCI is false)
                    {
                        await NewWorkflow.RunProjectStepAsync(steps, context, "trust", "Trusting the folder", projectSteps.TrustAsync, cancellationToken);
                    }

                    if (cli.Environment.IsCI is false && (ide is IdeLocator.VsCode || IdeLocator.FindVsCode(cli.Environment, cli.Runner) is not null))
                    {
                        await NewWorkflow.RunProjectStepAsync(steps, context, "vscode-extensions", "Installing VS Code extensions", projectSteps.VsCodeExtensionsAsync, cancellationToken);
                    }

                    if (sdkReady && runMode is not ProjectRunMode.None)
                    {
                        await ProjectRun.RunProjectAsync(cli, steps, context, projectSteps, runMode, aspire, cancellationToken);
                    }

                    if (sdkReady)
                    {
                        await NewWorkflow.RunNativeStepsAsync(cli, steps, context, projectSteps, parseResult.GetValue(shared.NoWorkloads), parseResult.GetValue(shared.NoRestore), parseResult.GetValue(shared.NoBuild), cancellationToken);
                    }
                }
                finally
                {
                    await AdministratorNeeds.EndAsync(cli);
                }

                if (ide is not IdeLocator.None)
                {
                    await NewWorkflow.RunProjectStepAsync(steps, context, "open", "Opening the IDE", (progress, ct) => projectSteps.OpenIdeAsync(ide, progress, ct, isTrusted: parseResult.GetValue(noTrust) is false), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (projectSteps.Running is not null || projectSteps.Workbench is not null || projectSteps.LandingPage is not null)
            {
                await ProjectRun.StopAsync(projectSteps.Running, projectSteps.Workbench, projectSteps.LandingPage);
                throw;
            }

            WriteSummary(cli, steps, name!, stopwatch.Elapsed);

            await ProjectRun.KeepRunningAsync(cli, projectSteps.Running, projectSteps.Workbench, projectSteps.LandingPage, cancellationToken);

            return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
        });

        return command;
    }

    private static string[] ForTheClone(IReadOnlyList<string> arguments, string address, string directory, IEnumerable<Platform>? askedPlatforms)
    {
        var forTheClone = arguments.Select(a => a == address ? directory : a).ToList();

        if (askedPlatforms is not null)
        {
            var separator = forTheClone.IndexOf("--");
            forTheClone.InsertRange(separator < 0 ? forTheClone.Count : separator, ["--platforms", string.Join(',', askedPlatforms.Order().Select(Platforms.Name))]);
        }

        return [.. forTheClone];
    }

    private static int WriteSummary(CliServices cli, StepRunner steps, string name, TimeSpan elapsed)
    {
        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine(steps.AnyFailed
            ? $"  [yellow bold]{Markup.Escape(name)} needs a look[/] [grey]in {CliConsole.FormatDuration(elapsed)}[/]"
            : $"  [green bold]{Markup.Escape(name)} is ready[/] [grey]in {CliConsole.FormatDuration(elapsed)}[/]");

        foreach (var followUp in steps.Reports.Select(r => r.Result).Where(r => r.FollowUp is not null && r.Status is StepStatus.Failed or StepStatus.Warning))
        {
            cli.Console.Out.MarkupLine($"    {Markup.Escape(followUp.FollowUp!)}");
        }

        return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
    }

    /// <summary>
    /// Git, and the GitHub CLI for a repository on github.com, before the clone: the rest of the project's tools are known only
    /// once it is cloned. The needed ones are installed without asking, as --yes does for the project's own; --tools narrows them.
    /// </summary>
    private static async Task InstallCloneToolsAsync(CliServices cli, StepRunner steps, RepositoryAddress repository, IReadOnlyList<string> requested, bool requestedGiven, CancellationToken cancellationToken)
    {
        var needs = new ToolNeeds { ClonesProject = true, CloneFromGitHub = repository.SignsInWithGitHubCli, Ide = IdeLocator.None };
        var context = NewWorkflow.CreateToolContext(cli, needs);
        var checks = await cli.Console.RunWithStatusAsync("Checking this machine for git", _ => ToolCatalog.CheckAsync(context, cancellationToken, ["git", "gh", "homebrew"]));
        var missing = checks.Where(c => c.Status.IsSatisfied is false && (requestedGiven is false || requested.Contains(c.Tool.Id, StringComparer.OrdinalIgnoreCase) || c.Tool.Id is "homebrew")).ToList();

        cli.Log.Write("Clone tools: " + string.Join(", ", checks.Select(c => $"{c.Tool.Id}={c.Status.State}")));

        if (requestedGiven && requested.Any(r => r.Equals("none", StringComparison.OrdinalIgnoreCase)))
            return;

        if (missing.Any(c => c.Tool.Id is not "homebrew"))
        {
            await new ToolInstaller(cli, steps).InstallAsync(missing, context, cancellationToken);
        }
    }

    /// <summary>
    /// Whether a folder is a clone of the repository itself, so bit setup sets it up instead of cloning it again: the root of
    /// its own git working tree, not a folder inside another one, whose origin as stored, before any insteadOf rewrite git
    /// applies when it reads it back, is the same repository.
    /// </summary>
    private static async Task<bool> IsCloneOfAsync(CliServices cli, string directory, RepositoryAddress repository, CancellationToken cancellationToken)
    {
        var root = await Git(["rev-parse", "--show-toplevel"]);

        if (root.Succeeded is false || string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root.Output.Trim())), Path.TrimEndingDirectorySeparator(directory), cli.Environment.IsLinux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) is false)
            return false;

        var origin = await Git(["config", "--get", "remote.origin.url"]);
        return origin.Succeeded && repository.IsSameAs(origin.Output.Trim());

        Task<ProcessResult> Git(string[] arguments) => cli.Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = arguments, WorkingDirectory = directory, Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);
    }

    /// <summary>VS Code when it's installed, but none in CI or in a dev container or Codespace, which opens the folder itself.</summary>
    private static string DefaultIde(CliServices cli, IReadOnlyList<ToolCheck> tools)
    {
        if (cli.Environment.IsCI || cli.Environment.GetVariable("DEVCONTAINER") is "true" || cli.Environment.GetVariable("CODESPACES") is "true")
            return IdeLocator.None;

        return IdeLocator.FindVsCode(cli.Environment, cli.Runner) is not null || tools.Any(t => t.Tool.Id is "vscode")
            ? IdeLocator.VsCode
            : IdeLocator.None;
    }
}
