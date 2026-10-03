using System.CommandLine;
using System.Text;
using Bit.Cli.Commands;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Spectre.Console;

namespace Bit.Cli;

public static class CliApp
{
    public const int ExitOk = 0;
    public const int ExitFailed = 1;
    public const int ExitUsage = 2;
    public const int ExitCheckFailed = 3;

    private static readonly string[] longRunningCommands = ["new", "setup", "translate", "doctor"];

    public static RootCommand BuildRootCommand(Func<CliServices> services)
    {
        var root = new RootCommand("bit CLI: create bit platform projects that are ready to run, translate .resx files and decode minified stack traces.")
        {
            NewCommand.Create(services),
            SetupCommand.Create(services),
            DoctorCommand.Create(services),
            TrustCommand.Create(services),
            TranslateCommand.Create(services),
            DecodeCommand.Create(services),
            TelemetryCommand.Create(services),
            AboutCommand.Create(services)
        };

        return root;
    }

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            Console.OutputEncoding = new UTF8Encoding(false);
        }
        catch (Exception exp) when (exp is IOException or PlatformNotSupportedException)
        {
        }

        var environment = CliEnvironment.FromProcess();
        var console = CliConsole.Create();
        CliServices? services = null;

        var root = BuildRootCommand(() => services!);
        var parseResult = root.Parse(args);
        var command = parseResult.CommandResult.Command;
        var commandName = command == root ? "bit" : command.Name;

        using var log = new CliLog(environment.LogsDirectory, commandName);
        services = new CliServices(environment, console, log: log);
        log.Write($"bit {BuildInfo.Version} ({BuildInfo.Commit ?? "local build"}) on {environment.Os} {environment.Architecture}: {string.Join(' ', args.Select(ProcessSpec.Quote))}");

        var isMeta = parseResult.Errors.Count > 0 || command == root || args.Any(a => a is "--help" or "-h" or "-?" or "/?" or "--version");
        var decision = TelemetryDecision.Resolve(environment, services.Settings, BuildInfo.TelemetryConnectionString);

        if (isMeta is false && commandName is not "telemetry" && TelemetryNotice.ShouldShow(decision, services.Settings))
        {
            TelemetryNotice.Show(commandName is "decode" ? console.Error : console.Out, services.Settings);
        }

        services.Telemetry = isMeta
            ? CliTelemetry.Disabled(decision)
            : CliTelemetry.Start(decision, environment, services.Settings, commandName, longRunningCommands.Contains(commandName), Console.Error);

        if (services.Settings.IsFirstRun)
        {
            services.Settings.Save();
        }

        var exitCode = ExitFailed;

        try
        {
            exitCode = await parseResult.InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
            return exitCode;
        }
        catch (OperationCanceledException)
        {
            console.Fail("Canceled.");
            return exitCode = 130;
        }
        catch (Exception exp)
        {
            services.Telemetry.RecordException(null, exp);
            log.Write(exp.ToString());

            console.Fail($"Something went wrong inside bit: {exp.Message}");
            if (log.FilePath is not null)
            {
                console.Error.MarkupLine($"  [grey]Log[/]       {Markup.Escape(log.FilePath)}");
            }

            if (services.Telemetry.ErrorId is { } errorId)
            {
                console.Error.MarkupLine($"  [grey]Error ID[/]  {errorId}  [grey]quote it in an issue: {BuildInfo.RepositoryUrl}/issues/new[/]");
            }

            return exitCode = ExitFailed;
        }
        finally
        {
            services.Telemetry.Complete(exitCode);
        }
    }
}
