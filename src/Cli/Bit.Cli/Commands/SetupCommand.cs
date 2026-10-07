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
        var pathArgument = new Argument<string?>("path") { Description = "The project folder. Default: the current folder.", Arity = ArgumentArity.ZeroOrOne };

        var command = new Command("setup", "Get an existing bit Boilerplate project ready on this machine, e.g. after cloning it or to add the Android app later: tools, workloads, packages and build.")
        {
            pathArgument
        };

        shared.AddTo(command);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();

            if (parseResult.GetValue(shared.Yes) || parseResult.GetValue(shared.NonInteractive))
            {
                cli.DisablePrompts();
            }

            var interactive = cli.IsInteractive && cli.Prompter.CanPrompt;
            var directory = Path.GetFullPath(parseResult.GetValue(pathArgument) ?? ".", cli.Environment.CurrentDirectory);
            var name = Directory.Exists(directory) ? ProjectContext.FindProjectName(directory) : null;

            if (name is null)
            {
                cli.Console.Fail($"{directory} isn't a bit Boilerplate project folder (no *.Web.slnf or *.slnx). Run bit setup in the project's folder.");
                return CliApp.ExitUsage;
            }

            var (platforms, error) = SharedOptions.ParsePlatforms(parseResult.GetValue(shared.Platforms), cli.Environment.Os);
            var (properties, propertyError) = SharedOptions.ParseProperties(parseResult.GetValue(shared.Properties));

            if ((error ?? propertyError) is { } usageError)
            {
                cli.Console.Fail(usageError);
                return CliApp.ExitUsage;
            }

            NewWorkflow.WriteHeader(cli);

            if (interactive && parseResult.GetResult(shared.Platforms) is null)
            {
                platforms = NewWorkflow.AskPlatforms(cli, platforms);
            }

            var context = new ProjectContext { Name = name, Directory = directory, Platforms = platforms, BuildProperties = properties };
            var aspire = Directory.Exists(Path.Combine(directory, "src", "Server", $"{name}.Server.AppHost"));
            var requirements = TemplateRequirements.FromProject(directory, name);
            var needs = new ToolNeeds { Aspire = aspire, NativeWebAssembly = ProjectContext.UsesNativeWebAssembly(directory), Platforms = platforms, Ide = IdeLocator.None, Sdk = requirements.Sdk, NodeMajor = requirements.NodeMajor, AspireVersion = requirements.Aspire };
            var hardware = NewWorkflow.ProbeHardwareAsync(cli, directory, cancellationToken);
            var tools = parseResult.GetValue(shared.NoTools)
                ? []
                : await NewWorkflow.ChooseToolsAsync(cli, needs, SharedOptions.SplitList(parseResult.GetValue(shared.Tools)), parseResult.GetResult(shared.Tools) is not null, interactive, hardware, cancellationToken);

            if (parseResult.GetValue(shared.NoTools) && hardware is not null)
            {
                NewWorkflow.WriteHardwareWarnings(cli, await hardware, needs);
            }

            cli.Telemetry.SetTag(Telemetry.TelemetryFields.Platforms, string.Join(',', platforms.Order().Select(Platforms.Name)));
            cli.Console.Out.WriteLine();

            var stopwatch = Stopwatch.StartNew();
            var steps = new StepRunner(cli);

            if (tools.Count > 0)
            {
                await new ToolInstaller(cli, steps).InstallAsync(tools, NewWorkflow.CreateToolContext(cli, needs), cancellationToken);
            }

            var projectSteps = new ProjectSteps(cli, context);
            var sdkReady = await NewWorkflow.RunSetupStepsAsync(cli, steps, context, projectSteps, parseResult.GetValue(shared.NoWorkloads), parseResult.GetValue(shared.NoRestore), parseResult.GetValue(shared.NoBuild), parseResult.GetValue(shared.NoBrowsers), cancellationToken);

            if (sdkReady && NewWorkflow.StartsAspireOnce(cli, aspire, parseResult.GetValue(shared.NoBuild)))
            {
                await NewWorkflow.RunProjectStepAsync(steps, context, "aspire-start", "Starting the project once with Aspire", projectSteps.AspireStartAsync, cancellationToken);
            }

            if (cli.Environment.IsCI is false && IdeLocator.FindVsCode(cli.Environment, cli.Runner) is not null)
            {
                await NewWorkflow.RunProjectStepAsync(steps, context, "vscode-extensions", "Installing VS Code extensions", projectSteps.VsCodeExtensionsAsync, cancellationToken);
            }

            cli.Console.Out.WriteLine();
            cli.Console.Out.MarkupLine(steps.AnyFailed
                ? $"  [yellow bold]{Markup.Escape(name)} needs a look[/] [grey]in {CliConsole.FormatDuration(stopwatch.Elapsed)}[/]"
                : $"  [green bold]{Markup.Escape(name)} is ready[/] [grey]in {CliConsole.FormatDuration(stopwatch.Elapsed)}[/]");

            foreach (var followUp in steps.Reports.Select(r => r.Result).Where(r => r.FollowUp is not null && r.Status is StepStatus.Failed or StepStatus.Warning))
            {
                cli.Console.Out.MarkupLine($"    {Markup.Escape(followUp.FollowUp!)}");
            }

            return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
        });

        return command;
    }
}
