using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Bit.Cli.Tools;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class DoctorCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var fixOption = new Option<bool>("--fix") { Description = "Offer to install what's missing." };
        var platformsOption = new Option<string[]>("--platforms") { Description = "Also check what these platforms need: android, ios, macos, windows.", AllowMultipleArgumentsPerToken = true, HelpName = "android,..." };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "With --fix, install what's needed without asking." };

        var command = new Command("doctor", "Check this machine for the tools a bit Boilerplate project needs. Inside a project, its own needs are checked.")
        {
            fixOption,
            platformsOption,
            yesOption
        };

        var projectOptions = new ProjectOptions();
        projectOptions.AddTo(command, projectOptions.Project, projectOptions.AppHost, projectOptions.ClientCore, projectOptions.Web);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();

            if (parseResult.GetValue(yesOption))
            {
                cli.DisablePrompts();
            }

            var (platforms, error) = SharedOptions.ParsePlatforms(parseResult.GetValue(platformsOption), cli.Environment.Os);

            if (error is not null)
            {
                return cli.UsageError("platforms-invalid", error);
            }

            var directory = projectOptions.Folder(parseResult, cli);
            var projectName = Directory.Exists(directory) ? ProjectContext.FindProjectName(directory) : null;
            var (parts, partsError) = projectOptions.Read(parseResult, cli, directory);

            if (partsError is not null)
            {
                return cli.UsageError("project-part-missing", partsError);
            }

            if (projectName is null && (projectOptions.IsGiven(parseResult) || parts.IsEmpty is false))
            {
                return cli.UsageError("not-a-project", $"{directory} isn't a bit Boilerplate project folder (no *.Web.slnf or *.slnx). {ProjectOptions.ProjectName} names the project's folder.");
            }

            var project = projectName is null ? null : ProjectContext.At(directory, projectName, parts);
            var aspire = project is null || Directory.Exists(project.AppHostDirectory);
            var requirements = project is null ? TemplateRequirements.Embedded.For(new TemplateSelection(TemplateManifest.Embedded)) : TemplateRequirements.FromProject(project);
            var needs = new ToolNeeds
            {
                Aspire = aspire,
                ContainersRequired = aspire && project is not null && ProjectContext.AppHostNeedsContainers(project.AppHostDirectory),
                NativeWebAssembly = project is not null && project.UsesNativeWebAssembly,
                Platforms = platforms,
                Ide = IdeLocator.FindAll(cli.Environment, cli.Runner).FirstOrDefault()?.Id,
                Sdk = requirements.Sdk,
                NodeMajor = requirements.NodeMajor,
                AspireVersion = requirements.Aspire
            };

            var context = NewWorkflow.CreateToolContext(cli, needs);
            var hardware = NewWorkflow.ProbeHardwareAsync(cli, directory, cancellationToken);
            var (checks, facts) = await cli.Console.RunWithStatusAsync("Checking this machine", async _ => (await ToolCatalog.CheckAsync(context, cancellationToken), hardware is null ? null : await hardware));

            cli.Console.Out.MarkupLine(projectName is null
                ? "[grey]Checked for a new project with Aspire and the web app. Run it in a project folder to check that project's own needs.[/]"
                : $"[grey]Checked for {Markup.Escape(projectName)}.[/]");

            foreach (var note in project?.PartsNotFound(settingUp: false) ?? [])
            {
                cli.Console.Warn(note);
            }

            var table = new Table().Border(TableBorder.Simple).BorderColor(Color.Grey)
                .AddColumn("")
                .AddColumn("Tool")
                .AddColumn("Found")
                .AddColumn("Why it matters");

            foreach (var check in checks)
            {
                var symbol = check.Status.IsSatisfied ? $"[green]{cli.Console.OkSymbol}[/]" : check.Needed ? $"[red]{cli.Console.FailSymbol}[/]" : $"[grey]{cli.Console.SkipSymbol}[/]";
                var found = check.Status.State switch
                {
                    ToolState.Installed => check.Status.Version ?? "yes",
                    ToolState.NotRunning => "not running",
                    ToolState.Outdated => $"{check.Status.Version} (too old)",
                    _ => "no"
                };

                table.AddRow(symbol, Markup.Escape(check.Tool.Name), Markup.Escape(found), $"[grey]{Markup.Escape(check.Why)}[/]");
            }

            cli.Console.Out.Write(table);

            if (facts is not null)
            {
                WriteHardware(cli, facts, needs);
                cli.Console.Out.WriteLine();
            }

            var missingNeeded = checks.Where(c => c.Status.IsSatisfied is false && c.Needed).ToList();

            if (parseResult.GetValue(fixOption) && checks.Any(c => c.Status.IsSatisfied is false))
            {
                var interactive = cli.IsInteractive && cli.Prompter.CanPrompt;
                var missing = checks.Where(c => c.Status.IsSatisfied is false).ToList();
                var selected = interactive
                    ? cli.Prompter.MultiSelect("Install these?", missing, missing.Where(c => c.Needed), c => NewWorkflow.DescribeTool(c))
                    : missingNeeded;

                if (selected.Count > 0)
                {
                    var steps = new StepRunner(cli);
                    await new ToolInstaller(cli, steps).InstallAsync(selected, context, cancellationToken);
                    return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
                }
            }

            if (missingNeeded.Count == 0)
            {
                cli.Console.Step(StepStatus.Succeeded, "Everything needed is here");
                return CliApp.ExitOk;
            }

            cli.Console.Out.MarkupLine($"[grey]Install what's missing with[/] bit doctor --fix");
            return CliApp.ExitCheckFailed;
        });

        return command;
    }

    private static void WriteHardware(CliServices cli, HardwareFacts facts, ToolNeeds needs)
    {
        var warnings = Hardware.Evaluate(facts, needs, cli.Environment.Os);

        if (facts.MemoryBytes is { } memory && warnings.Any(w => w.Id is "memory") is false)
        {
            cli.Console.Step(StepStatus.Succeeded, $"{Math.Round(memory / (1024d * 1024 * 1024))} GB of memory");
        }

        if (facts.VirtualizationEnabled is true)
        {
            cli.Console.Step(StepStatus.Succeeded, "Virtualization is on");
        }

        if (facts.OnHardDisk is false)
        {
            cli.Console.Step(StepStatus.Succeeded, "This folder's drive is an SSD");
        }

        NewWorkflow.WriteHardwareWarnings(cli, facts, needs);
    }
}
