using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class RenameCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var nameArgument = new Argument<string>("new-name") { Description = "The project's new name, e.g. Contoso.Shop." };
        var pathArgument = new Argument<string?>("path") { Description = "The project folder. Default: the current folder.", Arity = ArgumentArity.ZeroOrOne };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Rename a project that isn't in a git repository without asking." };

        var command = new Command("rename", "Rename a bit Boilerplate project: its files, folders, namespaces and every other place its name appears.")
        {
            nameArgument,
            pathArgument,
            yesOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var newName = parseResult.GetValue(nameArgument)?.Trim() ?? "";
            var folder = Path.GetFullPath(parseResult.GetValue(pathArgument) ?? ".", cli.Environment.CurrentDirectory);
            var oldName = Directory.Exists(folder) ? ProjectContext.FindProjectName(folder) : null;

            if (string.IsNullOrEmpty(oldName))
            {
                return cli.UsageError("not-a-project", $"{folder} isn't a bit Boilerplate project folder (no *.Web.slnf or *.slnx). Run bit rename in the project's folder.");
            }

            var typeNames = TypeNames.FromFolder(folder);

            if (ProjectName.Validate(newName, typeNames) is { } nameError)
            {
                return cli.UsageError("name-invalid", $"{nameError} Try: bit rename {ProjectName.Suggest(newName, typeNames)}");
            }

            if (string.Equals(oldName, newName, StringComparison.Ordinal))
            {
                cli.Console.Out.MarkupLine($"The project is already named [bold]{Markup.Escape(oldName)}[/].");
                return CliApp.ExitOk;
            }

            if (TypeNames.FindFile(folder, oldName) is { } typeFile)
            {
                return cli.UsageError("name-is-a-type", $"The project's name, {oldName}, is also the name of a type in its code, as in {Path.GetRelativePath(folder, typeFile)}, so bit rename can't tell which {oldName} to change. Rename that type first, or create the project again with bit new and another name.");
            }

            var renamer = new ProjectRenamer(oldName, newName);

            if (ProjectContext.ReadAppId(folder, oldName) is { } appId && ProjectName.IsValidAppId(appId) && ProjectName.IsValidAppId(renamer.Replace(appId)) is false)
            {
                return cli.UsageError("app-id-invalid", $"The rename would give the native apps the app id {renamer.Replace(appId)}, and each part of an app id must start with a Latin letter. Use Latin letters in the name, or set <ApplicationId> in {Platforms.MauiProject(oldName)} first.");
            }

            var inGit = false;
            var gitRoot = await cli.Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["rev-parse", "--show-toplevel"], WorkingDirectory = folder, Timeout = TimeSpan.FromMinutes(1) }, cancellationToken);

            if (gitRoot.Succeeded)
            {
                var status = await cli.Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["status", "--porcelain", "--", "."], WorkingDirectory = folder, Timeout = TimeSpan.FromMinutes(2) }, cancellationToken);
                inGit = status.Succeeded;

                if (inGit && string.IsNullOrWhiteSpace(status.Output) is false)
                {
                    return cli.UsageError("uncommitted-changes", $"{folder} has changes that aren't committed. Commit or stash them first, so the rename can be reviewed with git diff and undone with git checkout.");
                }
            }

            if (inGit is false && parseResult.GetValue(yesOption) is false)
            {
                if (cli.IsInteractive is false || cli.Prompter.CanPrompt is false)
                {
                    return cli.UsageError("needs-yes", $"{folder} isn't in a git repository, so the rename can't be undone with git. Pass --yes to rename it anyway.");
                }

                if (cli.Prompter.Confirm($"{folder} isn't in a git repository, so the rename can't be undone with git. Rename {oldName} to {newName} anyway?", false) is false)
                {
                    cli.Console.Dim("Nothing was changed.");
                    return CliApp.ExitOk;
                }
            }

            cli.Console.Out.MarkupLine($"Renaming [bold]{Markup.Escape(oldName)}[/] to [bold]{Markup.Escape(newName)}[/] [grey]in {Markup.Escape(folder)}[/]");

            var steps = new StepRunner(cli);
            var changedFiles = 0;
            var renamedFiles = 0;
            var renamedFolders = 0;

            var stoppedBuilds = await ProjectProcesses.StopAsync(cli, steps, folder, cancellationToken);

            await steps.RunAsync("rename-clean", "Deleting bin, obj and .vs folders", (_, token) => Task.Run(() =>
            {
                var (deleted, failures) = ProjectRenamer.DeleteBuildFolders(folder);
                var freed = FreedFiles.None;

                if (failures.Count > 0)
                {
                    freed = ProjectProcesses.Free(cli, failures.Select(f => f.Path), stoppedBuilds: stoppedBuilds);

                    if (freed.Stopped.Count > 0)
                    {
                        (var deletedAfter, failures) = ProjectRenamer.DeleteBuildFolders(folder);
                        deleted += deletedAfter;
                    }
                }

                Log(cli, failures);

                return failures.Count == 0
                    ? StepResult.Succeeded("Deleted bin, obj and .vs folders", freed.Stopped.Count > 0 ? $"{Count(deleted, "folder")}, after stopping {string.Join(", ", freed.Stopped)}" : Count(deleted, "folder"))
                    : StepResult.Warning("Couldn't delete every bin, obj and .vs folder", $"{Count(failures.Count, "folder")} left", hint: Hint(failures, freed));
            }, token), cancellationToken);

            var contents = await steps.RunAsync("rename-contents", "Replacing the name in files", (_, token) => Task.Run(() =>
            {
                var (changed, failures) = renamer.ReplaceInFiles(folder);
                changedFiles = changed;
                Log(cli, failures);

                return failures.Count == 0
                    ? StepResult.Succeeded("Replaced the name in files", Count(changed, "file"))
                    : StepResult.Failed("Couldn't replace the name in every file", $"{Count(failures.Count, "file")} failed", hint: Hint(failures, ProjectProcesses.Free(cli, failures.Select(f => f.Path), stopBuildProcesses: false)));
            }, token), cancellationToken);

            if (contents.Status is StepStatus.Failed)
            {
                steps.Add("rename-paths", StepResult.Skipped("Didn't rename files and folders", "some files still have the old name"));
            }
            else
            {
                await steps.RunAsync("rename-paths", "Renaming files and folders", (_, token) => Task.Run(() =>
                {
                    var (files, folders, failures) = renamer.RenamePaths(folder);
                    renamedFiles = files;
                    renamedFolders = folders;
                    Log(cli, failures);

                    return failures.Count == 0
                        ? StepResult.Succeeded("Renamed files and folders", $"{Count(files, "file")}, {Count(folders, "folder")}")
                        : StepResult.Failed("Couldn't rename every file and folder", $"{Count(failures.Count, "item")} failed", hint: Hint(failures, ProjectProcesses.Free(cli, failures.Select(f => f.Path), stopBuildProcesses: false)));
                }, token), cancellationToken);
            }

            cli.Console.Out.WriteLine();
            cli.Console.Out.MarkupLine(steps.AnyFailed
                ? $"  [yellow bold]The rename to {Markup.Escape(newName)} needs a look[/]"
                : $"  [green bold]{Markup.Escape(oldName)} is now {Markup.Escape(newName)}[/]");
            cli.Console.Out.MarkupLine($"    {Count(changedFiles, "file")} changed, {Count(renamedFiles, "file")} and {Count(renamedFolders, "folder")} renamed.");

            if (steps.AnyFailed && cli.Log.FilePath is { } logPath)
            {
                cli.Console.Out.MarkupLine($"    [grey]Every failure is in the log:[/] {Markup.Escape(logPath)}");
            }

            cli.Console.Out.MarkupLine(inGit
                ? "    Next: review the changes with [bold]git diff[/], then run [bold]bit setup[/] to restore and build."
                : "    Next: run [bold]bit setup[/] to restore and build.");

            if (string.Equals(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)), oldName, StringComparison.OrdinalIgnoreCase))
            {
                cli.Console.Out.MarkupLine($"    [grey]The project folder is still named {Markup.Escape(oldName)}. Rename it after closing the editors and terminals that are in it.[/]");
            }

            return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
        });

        return command;
    }

    private static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";

    private static string Describe(PathFailure failure) => $"{failure.Path}: {failure.Reason}";

    private static string Hint(IReadOnlyList<PathFailure> failures, FreedFiles freed) => freed.InUseBy is { } holders ? $"{Describe(failures[0])}\n{holders}" : Describe(failures[0]);

    private static void Log(CliServices cli, IReadOnlyList<PathFailure> failures)
    {
        foreach (var failure in failures)
        {
            cli.Log.Write($"rename: {Describe(failure)}");
        }
    }
}
