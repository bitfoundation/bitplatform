using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class RemoveCommand
{
    private const int ShownFailures = 3;

    public static Command Create(Func<CliServices> services)
    {
        var pathArgument = new Argument<string?>("path") { Description = "The project folder. Default: the current folder.", Arity = ArgumentArity.ZeroOrOne };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Delete it without asking." };

        var command = new Command("remove", "Delete a bit Boilerplate project folder for good, after stopping the apps and build servers that keep its files in use.")
        {
            pathArgument,
            yesOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parseResult.GetValue(pathArgument) ?? ".", cli.Environment.CurrentDirectory));
            var name = Directory.Exists(folder) ? ProjectContext.FindProjectName(folder) : null;

            if (string.IsNullOrEmpty(name) || IsProjectRoot(folder, name) is false)
            {
                cli.Console.Fail($"{folder} isn't a bit Boilerplate project folder (no *.Web.slnf or src/Client/*.Client.Core). Run bit remove in the project's folder, or pass its path.");
                return CliApp.ExitUsage;
            }

            if (Path.GetDirectoryName(folder) is null || string.Equals(folder, Path.TrimEndingDirectorySeparator(cli.Environment.HomeDirectory), StringComparison.OrdinalIgnoreCase))
            {
                cli.Console.Fail($"{folder} is a drive's root or your home folder, so bit remove won't delete it.");
                return CliApp.ExitUsage;
            }

            var losses = await FindLossesAsync(cli, folder, cancellationToken);

            if (parseResult.GetValue(yesOption))
            {
                WriteLosses(cli, losses);
            }
            else if (cli.IsInteractive is false || cli.Prompter.CanPrompt is false)
            {
                WriteLosses(cli, losses);
                cli.Console.Fail($"Deleting {folder} can't be undone. Pass --yes to delete it.");
                return CliApp.ExitUsage;
            }
            else if (cli.Prompter.Confirm(string.Join('\n', losses.Append($"Delete {folder} and everything in it?")), false) is false)
            {
                cli.Console.Dim("Nothing was changed.");
                return CliApp.ExitOk;
            }

            cli.Console.Out.MarkupLine($"Removing [bold]{Markup.Escape(name)}[/] [grey]in {Markup.Escape(folder)}[/]");

            var steps = new StepRunner(cli);
            var onlyEmptyFoldersLeft = false;

            await ProjectProcesses.StopAsync(cli, steps, folder, cancellationToken);
            LeaveFolder(folder);

            var result = await steps.RunAsync("remove", "Deleting the project folder", (_, token) => Task.Run(() =>
            {
                var (failures, emptyFoldersLeft) = FolderRemover.Delete(folder);
                onlyEmptyFoldersLeft = emptyFoldersLeft;

                foreach (var failure in failures)
                {
                    cli.Log.Write($"remove: {failure.Path}: {failure.Reason}");
                }

                if (failures.Count > 0)
                {
                    var shown = string.Join('\n', failures.Take(ShownFailures).Select(f => f.Path));
                    var more = failures.Count > ShownFailures ? $"\nand {failures.Count - ShownFailures} more" : "";
                    return StepResult.Failed("Couldn't delete everything", $"{failures.Count} item{(failures.Count == 1 ? "" : "s")} left", hint: shown + more);
                }

                return emptyFoldersLeft
                    ? StepResult.Warning("Deleted everything in the folder", "a terminal is still in it")
                    : StepResult.Succeeded("Deleted the project folder");
            }, token), cancellationToken);

            cli.Console.Out.WriteLine();

            if (result.Status is StepStatus.Failed)
            {
                cli.Console.Out.MarkupLine($"  [yellow bold]Some of {Markup.Escape(name)} is left[/]");
                cli.Console.Out.MarkupLine("    Close the apps that use these files, then run bit remove again.");

                if (cli.Log.FilePath is { } logPath)
                {
                    cli.Console.Out.MarkupLine($"    [grey]Every file that's left is in the log:[/] {Markup.Escape(logPath)}");
                }

                return CliApp.ExitFailed;
            }

            cli.Console.Out.MarkupLine($"  [green bold]Removed {Markup.Escape(name)}[/]");

            if (onlyEmptyFoldersLeft)
            {
                cli.Console.Out.MarkupLine($"    [grey]{Markup.Escape(folder)} stays, empty, while a terminal is in it. Delete it after leaving it.[/]");
            }

            return CliApp.ExitOk;
        });

        return command;
    }

    private static bool IsProjectRoot(string folder, string name)
    {
        return File.Exists(Path.Combine(folder, $"{name}.Web.slnf")) || Directory.Exists(Path.Combine(folder, "src", "Client", $"{name}.Client.Core"));
    }

    private static async Task<List<string>> FindLossesAsync(CliServices cli, string folder, CancellationToken cancellationToken)
    {
        var losses = new List<string>();

        if ((await Git(cli, folder, ["rev-parse", "--show-toplevel"], cancellationToken)).Succeeded is false)
            return losses;

        var status = await Git(cli, folder, ["status", "--porcelain", "--", "."], cancellationToken);

        if (status.Succeeded && string.IsNullOrWhiteSpace(status.Output) is false)
        {
            losses.Add("It has changes that aren't committed.");
        }

        if (Directory.Exists(Path.Combine(folder, ".git")) is false)
            return losses;

        var ahead = await Git(cli, folder, ["rev-list", "--count", "@{u}..HEAD"], cancellationToken);

        if (ahead.Succeeded is false)
        {
            losses.Add("Its branch has no upstream, so its commits may exist only here.");
        }
        else if (int.TryParse(ahead.Output.Trim(), out var count) && count > 0)
        {
            losses.Add(count == 1 ? "Its branch has 1 commit that isn't pushed." : $"Its branch has {count} commits that aren't pushed.");
        }

        return losses;
    }

    private static Task<ProcessResult> Git(CliServices cli, string folder, string[] arguments, CancellationToken cancellationToken)
    {
        return cli.Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = arguments, WorkingDirectory = folder, Timeout = TimeSpan.FromMinutes(1) }, cancellationToken);
    }

    private static void WriteLosses(CliServices cli, IEnumerable<string> losses)
    {
        foreach (var loss in losses)
        {
            cli.Console.Warn(loss);
        }
    }

    private static void LeaveFolder(string folder)
    {
        try
        {
            var current = Path.TrimEndingDirectorySeparator(Environment.CurrentDirectory);

            if (string.Equals(current, folder, StringComparison.OrdinalIgnoreCase) || ProjectProcesses.IsUnder(current, folder))
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(folder)!;
            }
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }
    }
}
