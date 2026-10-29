using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class MigrationCommand
{
    public static Command Create(Func<CliServices> services)
    {
        return new Command("migration", "Add and remove the EF Core migrations of a bit Boilerplate project's database or of its offline database, and bundle them for a deployed database. It works in any folder of the project.")
        {
            Add(services),
            Remove(services),
            Optimize(services),
            Bundle(services)
        };
    }

    private static Command Add(Func<CliServices> services)
    {
        var nameArgument = new Argument<string>("name") { Description = "The migration's name, e.g. AddProductRating." };
        var offlineOption = new Option<bool>("--offline") { Description = "Add it to the offline database in Client.Core instead, then regenerate that database's compiled model." };

        var command = new Command("add", "Add a migration with what changed in the database's model since the last one.")
        {
            nameArgument,
            offlineOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameArgument)?.Trim() ?? "";
            var offline = parseResult.GetValue(offlineOption);

            return RunAsync(services(), offline, async (steps, migrations) =>
            {
                var before = migrations.MigrationFiles(offline);
                var added = await steps.RunAsync("migration-add", $"Adding the {name} migration", (progress, token) => migrations.AddAsync(name, offline, progress, token), cancellationToken);

                if (added.Status is not StepStatus.Failed)
                {
                    await steps.RunAsync("migration-format", "Formatting the migration", (progress, token) => migrations.FormatAsync(offline, before, progress, token), cancellationToken);
                }

                if (offline)
                {
                    await OptimizeAfterAsync(steps, migrations, added, "the migration wasn't added", cancellationToken);
                }
            });
        });

        return command;
    }

    private static Command Remove(Func<CliServices> services)
    {
        var offlineOption = new Option<bool>("--offline") { Description = "Remove it from the offline database in Client.Core instead, then regenerate that database's compiled model." };

        var command = new Command("remove", "Remove the last migration, as long as the development database doesn't have it yet.")
        {
            offlineOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var offline = parseResult.GetValue(offlineOption);

            return RunAsync(services(), offline, async (steps, migrations) =>
            {
                var removed = await steps.RunAsync("migration-remove", "Removing the last migration", (progress, token) => migrations.RemoveAsync(offline, progress, token), cancellationToken);

                if (offline)
                {
                    await OptimizeAfterAsync(steps, migrations, removed, "the migration wasn't removed", cancellationToken);
                }
            });
        });

        return command;
    }

    private static Command Optimize(Func<CliServices> services)
    {
        var command = new Command("optimize", "Regenerate the offline database's compiled model, which the app needs outside Development, e.g. before the first publish. add and remove with --offline do it for you.");

        command.SetAction((_, cancellationToken) => RunAsync(services(), offline: true, (steps, migrations) =>
            steps.RunAsync("migration-optimize", "Regenerating the offline database's compiled model", (progress, token) => migrations.OptimizeAsync(build: true, progress, token), cancellationToken)));

        return command;
    }

    private static Command Bundle(Func<CliServices> services)
    {
        var runtimeOption = new Option<string>("--runtime", "-r") { Description = "The runtime of the machine the bundle runs on, e.g. linux-x64, linux-arm64, win-x64 or osx-arm64.", DefaultValueFactory = _ => "linux-x64" };

        var command = new Command("bundle", "Build efbundle in Server.Api: one executable that applies the migrations to a deployed database.")
        {
            runtimeOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var cli = services();
            var runtime = parseResult.GetValue(runtimeOption)?.Trim() is { Length: > 0 } value ? value : "linux-x64";

            return RunAsync(cli, offline: false, async (steps, migrations) =>
            {
                var bundle = await steps.RunAsync("migration-bundle", $"Building the migrations bundle for {runtime}", (progress, token) => migrations.BundleAsync(runtime, progress, token), cancellationToken);

                if (bundle.Status is not StepStatus.Failed)
                {
                    cli.Console.Out.MarkupLine($"    Copy {Markup.Escape(migrations.BundlePath(runtime))} into the deployed app's folder, where it finds the app's settings, and run it there before the new version starts.");
                }
            });
        });

        return command;
    }

    private static async Task OptimizeAfterAsync(StepRunner steps, ProjectMigrations migrations, StepResult previous, string reason, CancellationToken cancellationToken)
    {
        if (previous.Status is StepStatus.Failed)
        {
            steps.Add("migration-optimize", StepResult.Skipped("Didn't regenerate the compiled model", reason));
            return;
        }

        await steps.RunAsync("migration-optimize", "Regenerating the offline database's compiled model", (progress, token) => migrations.OptimizeAsync(build: false, progress, token), cancellationToken);
    }

    private static async Task<int> RunAsync(CliServices cli, bool offline, Func<StepRunner, ProjectMigrations, Task> work)
    {
        var directory = ProjectContext.FindProjectDirectory(cli.Environment.CurrentDirectory);

        if (directory is null || ProjectContext.FindProjectName(directory) is not { } name)
        {
            return cli.UsageError("not-a-project", $"{cli.Environment.CurrentDirectory} isn't in a bit Boilerplate project: neither it nor a folder above it has a *.Web.slnf or *.slnx. Run bit migration in the project's folder.");
        }

        var project = new ProjectContext { Name = name, Directory = directory, Platforms = new HashSet<Platform> { Platform.Web } };
        var migrations = new ProjectMigrations(cli, project);

        if (offline && migrations.HasOfflineDatabase is false)
        {
            return cli.UsageError("offline-db-missing", $"{name} has no offline database. Only a project created with --offlineDb true has one.");
        }

        if (offline is false && Directory.Exists(project.ServerApiDirectory) is false)
        {
            return cli.UsageError("server-api-missing", $"{name} has no src/Server/{name}.Server.Api folder, where the database's migrations are.");
        }

        if (offline is false && migrations.HasDatabaseProvider is false)
        {
            return cli.UsageError("database-provider-missing", $"{name} was created with --database Other, so its database has no provider yet. Add the EF Core provider of your database (https://learn.microsoft.com/ef/core/providers) to Server.Api and use it in Program.Services.cs, where the template throws NotImplementedException, then run this again.");
        }

        cli.Console.Out.MarkupLine($"[bold]{Markup.Escape(name)}[/]{(offline ? "'s offline database" : "'s database")} [grey]in {Markup.Escape(directory)}[/]");

        var steps = new StepRunner(cli);
        await work(steps, migrations);

        var failed = steps.Reports.Where(r => r.Result is { Status: StepStatus.Failed or StepStatus.Warning, FollowUp: not null }).ToList();

        if (failed.Count > 0)
        {
            cli.Console.Out.WriteLine();
            cli.Console.Out.MarkupLine("  [bold]To try again[/]");

            foreach (var report in failed)
            {
                cli.Console.Out.MarkupLine($"    {Markup.Escape(report.Result.FollowUp!)}");
            }

            if (cli.Log.FilePath is { } logPath)
            {
                cli.Console.Out.MarkupLine($"    [grey]All that dotnet ef wrote is in the log:[/] {Markup.Escape(logPath)}");
            }
        }

        return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
    }
}
