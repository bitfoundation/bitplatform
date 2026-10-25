using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class TelemetryCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var levelArgument = new Argument<string?>("level")
        {
            Description = "off, errors (the default: error reports only) or all (error reports plus the options you pick).",
            Arity = ArgumentArity.ZeroOrOne
        };
        levelArgument.AcceptOnlyFromAmong("off", "errors", "all");

        var command = new Command("telemetry", "Show what bit reports to the bit platform team, and change it.")
        {
            levelArgument
        };

        command.SetAction(parseResult =>
        {
            var cli = services();

            if (parseResult.GetValue(levelArgument) is { } level)
            {
                cli.Settings.Telemetry = level;

                if (cli.Settings.Save() is false)
                {
                    cli.Console.Fail($"Couldn't write {Path.Combine(cli.Environment.BitDirectory, "settings.json")}.");
                    return CliApp.ExitFailed;
                }
            }

            var decision = TelemetryDecision.Resolve(cli.Environment, cli.Settings, BuildInfo.TelemetryConnectionString);
            var name = TelemetryDecision.Name(decision.Level);

            cli.Console.Out.MarkupLine($"Telemetry is [bold]{name}[/] [grey](set by {Markup.Escape(decision.Reason)})[/]");
            cli.Console.Out.MarkupLine(decision.Level switch
            {
                TelemetryLevel.Off => "[grey]Nothing is sent.[/]",
                TelemetryLevel.Errors => "[grey]Sent: whether each step worked, error codes, crash types and stack traces, OS and versions. Never names, paths or file contents.[/]",
                _ => "[grey]Sent: the error reports, plus the template options, platforms and tools you pick, and translation volumes.[/]"
            });

            if (decision.CanSend is false && decision.Level is not TelemetryLevel.Off)
            {
                cli.Console.Out.MarkupLine("[grey]This build has no telemetry endpoint, so it sends nothing anyway.[/]");
            }

            if (parseResult.GetValue(levelArgument) is { } requested && TelemetryDecision.ParseLevel(requested) != decision.Level)
            {
                cli.Console.Warn($"{decision.Reason} overrides the setting you just saved.");
            }

            cli.Console.Out.MarkupLine($"[grey]Change it with[/] bit telemetry off|errors|all[grey], or BIT_CLI_TELEMETRY. DO_NOT_TRACK=1 turns everything off. Details: {Markup.Escape(TelemetryNotice.DocsUrl)}[/]");
            return CliApp.ExitOk;
        });

        return command;
    }
}
