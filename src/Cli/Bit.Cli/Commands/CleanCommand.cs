using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class CleanCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var pathArgument = new Argument<string?>("path") { Description = "The project folder. Default: the current folder.", Arity = ArgumentArity.ZeroOrOne };

        var command = new Command("clean", "Delete the project's build outputs and downloaded packages (bin, obj, node_modules and generated css/js) so the next build starts from scratch.")
        {
            pathArgument
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var folder = Path.GetFullPath(parseResult.GetValue(pathArgument) ?? ".", cli.Environment.CurrentDirectory);
            var name = Directory.Exists(folder) ? ProjectContext.FindProjectName(folder) : null;

            if (name is null)
            {
                cli.Console.Fail($"{folder} isn't a bit Boilerplate project folder (no *.Web.slnf or *.slnx). Run bit clean in the project's folder.");
                return CliApp.ExitUsage;
            }

            var script = cli.Environment.IsWindows ? "Clean.bat" : "Clean.sh";

            if (File.Exists(Path.Combine(folder, script)) is false)
            {
                cli.Console.Fail($"{folder} has no {script} to clean the project with.");
                return CliApp.ExitUsage;
            }

            cli.Console.Out.MarkupLine($"Cleaning [bold]{Markup.Escape(name)}[/] [grey]in {Markup.Escape(folder)}[/]");

            var steps = new StepRunner(cli);
            await ProjectProcesses.StopAsync(cli, steps, folder, cancellationToken);

            var spec = new ProcessSpec
            {
                FileName = cli.Environment.IsWindows ? "cmd.exe" : "bash",
                Arguments = cli.Environment.IsWindows ? ["/c", script] : [script],
                WorkingDirectory = folder,
                Timeout = TimeSpan.FromMinutes(30),
                Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1", ["MSBUILDTERMINALLOGGER"] = "off" }
            };

            Task<StepResult> RunScriptAsync(string id, string title) => steps.RunAsync(id, title, async (progress, token) =>
                StepResult.FromProcess(await cli.Runner.RunAsync(spec with { OnOutputLine = progress }, token), "Cleaned the project", $"{script} failed",
                    "Close the IDEs and apps that use the project, then run bit clean again."), cancellationToken);

            var result = await RunScriptAsync("clean", $"Running {script}");

            if (cli.Environment.IsWindows && ProjectRenamer.FindBuildFolders(folder) is { Count: > 0 } left)
            {
                var freed = ProjectProcesses.Free(cli, left);

                if (freed.Stopped.Count > 0)
                {
                    result = await RunScriptAsync("clean-again", $"Running {script} again after stopping {string.Join(", ", freed.Stopped)}");
                    left = ProjectRenamer.FindBuildFolders(folder);
                    freed = left.Count > 0 ? ProjectProcesses.Free(cli, left, stopBuildProcesses: false) : FreedFiles.None;
                }

                if (left.Count > 0 && result.Status is not StepStatus.Failed)
                {
                    var shown = string.Join('\n', left.Take(5)) + (freed.InUseBy is { } holders ? $"\n{holders}" : "");
                    steps.Add("clean-left", StepResult.Warning("Some build folders are still there", $"{left.Count} folder{(left.Count == 1 ? "" : "s")}", hint: shown));
                }
            }

            if (result.Status is StepStatus.Failed && result.FollowUp is not null)
            {
                cli.Console.Out.MarkupLine($"    {Markup.Escape(result.FollowUp)}");
            }

            return result.Status is StepStatus.Failed ? CliApp.ExitFailed : CliApp.ExitOk;
        });

        return command;
    }
}
