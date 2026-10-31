using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class RunCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var platformArgument = new Argument<string?>("platform")
        {
            Description = "web (the default), android, ios, macos or windows. android uses a connected device or a running emulator, or else sets up and starts the Android emulator.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var propertyOption = new Option<string[]>("--property", "-p")
        {
            Description = "An MSBuild property for the build, e.g. -p:Environment=Staging. Repeat it for more.",
            HelpName = "name=value"
        };

        var command = new Command("run", "Build and start the bit Boilerplate project in this folder: the web app and its server, or the Android, iOS, macOS or Windows app.")
        {
            platformArgument,
            propertyOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var value = parseResult.GetValue(platformArgument);

            if ((value is null ? Platform.Web : Platforms.Parse(value)) is not { } platform)
            {
                return cli.UsageError("platform-invalid", $"'{value}' isn't a platform. Use web, android, ios, macos or windows.");
            }

            if (Platforms.RunnableOn(cli.Environment.Os).Contains(platform) is false)
            {
                return cli.UsageError("platform-unavailable", platform switch
                {
                    Platform.Ios => "The iOS app runs in the iOS simulator, which needs macOS.",
                    Platform.MacOS => "The macOS app runs on macOS only.",
                    _ => "The Windows app runs on Windows only."
                });
            }

            var (properties, propertyError) = SharedOptions.ParseProperties(parseResult.GetValue(propertyOption));

            if (propertyError is not null)
            {
                return cli.UsageError("properties-invalid", propertyError);
            }

            var directory = cli.Environment.CurrentDirectory;

            if (ProjectContext.FindProjectName(directory) is not { } name)
            {
                return cli.UsageError("not-a-project", $"{directory} isn't a bit Boilerplate project folder (no *.Web.slnf or *.slnx). Run bit run in the project's folder.");
            }

            var app = platform switch
            {
                Platform.Web => null,
                Platform.Windows => Platforms.WindowsProject(name),
                _ => Platforms.MauiProject(name)
            };

            if (app is not null && File.Exists(Path.Combine(directory, app)) is false)
            {
                return cli.UsageError("app-missing", $"{name} has no {app}, so it has no {Platforms.Title(platform)} app to run.");
            }

            var context = new ProjectContext { Name = name, Directory = directory, Platforms = new HashSet<Platform> { Platform.Web, platform }, BuildProperties = properties };
            var steps = new StepRunner(cli);
            var projectSteps = new ProjectSteps(cli, context);

            NewWorkflow.WriteHeader(cli);
            cli.Telemetry.SetTag(Telemetry.TelemetryFields.Platforms, Platforms.Name(platform));

            await NewWorkflow.RunProjectStepAsync(steps, context, "sdk", "Checking the .NET SDK", projectSteps.SdkAsync, cancellationToken);

            if (steps.AnyFailed is false && platform is Platform.Web)
            {
                var aspire = Directory.Exists(context.AppHostDirectory);

                if (aspire is false)
                {
                    await NewWorkflow.RunProjectStepAsync(steps, context, "build", "Building the web app", projectSteps.BuildAsync, cancellationToken);
                }

                await ProjectRun.RunProjectAsync(cli, steps, context, projectSteps, ProjectRunMode.UntilClosed, aspire, cancellationToken);
            }
            else if (steps.AnyFailed is false)
            {
                await new NativeRun(cli, context).RunAsync(steps, platform, cancellationToken);
            }

            var followUps = steps.Reports.Select(r => r.Result).Where(r => r.FollowUp is not null && r.Status is StepStatus.Failed or StepStatus.Warning).ToList();

            if (followUps.Count > 0)
            {
                cli.Console.Out.WriteLine();

                foreach (var followUp in followUps)
                {
                    cli.Console.Out.MarkupLine($"    {Markup.Escape(followUp.FollowUp!)}");
                }
            }

            await ProjectRun.KeepRunningAsync(cli, projectSteps.Running, null, cancellationToken);

            return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
        });

        return command;
    }
}
