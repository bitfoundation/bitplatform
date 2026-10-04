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

            if (error is not null)
            {
                cli.Console.Fail(error);
                return CliApp.ExitUsage;
            }

            NewWorkflow.WriteHeader(cli);

            if (interactive && parseResult.GetResult(shared.Platforms) is null)
            {
                platforms = NewWorkflow.AskPlatforms(cli, platforms);
            }

            var context = new ProjectContext { Name = name, Directory = directory, Platforms = platforms };
            var aspire = Directory.Exists(Path.Combine(directory, "src", "Server", $"{name}.Server.AppHost"));
            var needs = new ToolNeeds { Aspire = aspire, NativeWebAssembly = ProjectContext.UsesNativeWebAssembly(directory), Platforms = platforms, Ide = IdeLocator.None, MinimumSdk = ReadMinimumSdk(directory) };
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
            await NewWorkflow.RunSetupStepsAsync(cli, steps, context, projectSteps, parseResult.GetValue(shared.NoWorkloads), parseResult.GetValue(shared.NoRestore), parseResult.GetValue(shared.NoBuild), cancellationToken);

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

    public static Version? ReadMinimumSdk(string directory)
    {
        var path = Path.Combine(directory, "global.json");

        try
        {
            if (File.Exists(path) is false)
                return null;

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path), new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
            return document.RootElement.TryGetProperty("sdk", out var sdk) && sdk.TryGetProperty("version", out var version) && Version.TryParse(version.GetString(), out var parsed) ? parsed : null;
        }
        catch (Exception exp) when (exp is IOException or System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
