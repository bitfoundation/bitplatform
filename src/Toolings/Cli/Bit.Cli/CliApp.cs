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

    private static readonly string[] longRunningCommands = ["new", "setup", "run", "diff", "translate", "doctor", "clean", "aot-profile", "migration", "pipeline"];

    public static RootCommand BuildRootCommand(Func<CliServices> services)
    {
        var root = new RootCommand("bit CLI: create bit platform projects that are ready to run, translate .resx files and decode minified stack traces.")
        {
            NewCommand.Create(services),
            SetupCommand.Create(services),
            RunCommand.Create(services),
            DiffCommand.Create(services),
            DoctorCommand.Create(services),
            TrustCommand.Create(services),
            CleanCommand.Create(services),
            RenameCommand.Create(services),
            RemoveCommand.Create(services),
            MigrationCommand.Create(services),
            TranslateCommand.Create(services),
            DecodeCommand.Create(services),
            KeysCommand.Create(services),
            PipelineCommand.Create(services),
            AotProfileCommand.Create(services),
            TelemetryCommand.Create(services),
            UpdateCommand.Create(services),
            AboutCommand.Create(services),
            AdminHelperCommand.Create(services)
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
        Environment.SetEnvironmentVariable(SelfUpdate.HandedOffVariable, null);
        var console = CliConsole.Create();
        CliServices? services = null;

        var root = BuildRootCommand(() => services!);
        var parseResult = root.Parse(args);
        var command = parseResult.CommandResult.Command;
        var commandPath = CommandPath(parseResult.CommandResult, root);
        var commandName = commandPath.Count == 0 ? "bit" : string.Join('-', commandPath);
        var topCommandName = commandPath.Count == 0 ? "bit" : commandPath[0];

        using var log = new CliLog(environment.LogsDirectory, commandName);
        services = new CliServices(environment, console, log: log);
        log.Write($"bit {BuildInfo.Version} ({BuildInfo.Commit ?? "local build"}) on {environment.Os} {environment.Architecture}: {string.Join(' ', args.Select(ProcessSpec.Quote))}");

        var isMeta = parseResult.Errors.Count > 0 || command == root || commandName is Tools.AdministratorSession.CommandName || args.Any(a => a is "--help" or "-h" or "-?" or "/?" or "--version");
        var decision = TelemetryDecision.Resolve(environment, services.Settings, BuildInfo.TelemetryConnectionString);

        if (isMeta is false && commandName is not "telemetry" && TelemetryNotice.ShouldShow(decision, services.Settings))
        {
            TelemetryNotice.Show(commandName is "decode" ? console.Error : console.Out, services.Settings);
        }

        services.Telemetry = isMeta
            ? CliTelemetry.Disabled(decision)
            : CliTelemetry.Start(decision, environment, services.Settings, commandName, longRunningCommands.Contains(topCommandName), Console.Error);

        if (services.Settings.IsFirstRun)
        {
            services.Settings.Save();
        }

        using var title = new ConsoleTitle(isMeta is false && console.IsInteractive && Console.IsOutputRedirected is false && environment.IsCI is false, console.Unicode, $"bit {string.Join(' ', commandPath)}");
        console.Title = title;

        var exitCode = ExitFailed;

        try
        {
            exitCode = await parseResult.InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false, ProcessTerminationTimeout = TimeSpan.FromSeconds(10) });
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

    public static IReadOnlyList<string> CommandPath(System.CommandLine.Parsing.CommandResult result, Command root)
    {
        var names = new List<string>();

        for (var current = result; current is not null && current.Command != root; current = current.Parent as System.CommandLine.Parsing.CommandResult)
        {
            names.Insert(0, current.Command.Name);
        }

        return names;
    }
}
