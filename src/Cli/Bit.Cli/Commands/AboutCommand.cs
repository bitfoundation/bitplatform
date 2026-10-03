using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class AboutCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var command = new Command("about", "Show where this build of bit comes from, how to verify it, and what a bug report needs.");

        command.SetAction(_ =>
        {
            Write(services());
            return CliApp.ExitOk;
        });

        return command;
    }

    public static void Write(CliServices cli)
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap().PadRight(2)).AddColumn();

        void Row(string name, string value) => grid.AddRow($"[grey]{Markup.Escape(name)}[/]", Markup.Escape(value));

        Row("Version", BuildInfo.Commit is null ? BuildInfo.Version : $"{BuildInfo.Version} (commit {BuildInfo.ShortCommit})");
        Row("Source", $"{BuildInfo.SourceUrl}  (MIT license)");
        Row("Built by", BuildInfo.RunUrl is { } runUrl ? $"GitHub Actions {runUrl}" : "a local build, not GitHub Actions");
        Row("Package", BuildInfo.PackageUrl);

        if (BuildInfo.IsOfficialBuild)
        {
            Row("Verify", $"gh attestation verify {ProcessSpec.Quote(typeof(BuildInfo).Assembly.Location)} --repo bitfoundation/bitplatform");
        }

        Row("Runs on", $".NET {Environment.Version} on {OsName(cli.Environment.Os)} {Environment.OSVersion.Version} ({cli.Environment.Architecture.ToString().ToLowerInvariant()})");
        Row("Installed as", CliTelemetry.InstallMethod() switch { "tool" => ".NET global tool", "dnx" => "dnx (not installed)", _ => "a local build" });

        var decision = TelemetryDecision.Resolve(cli.Environment, cli.Settings, BuildInfo.TelemetryConnectionString);
        Row("Telemetry", $"{TelemetryDecision.Name(decision.Level)} ({decision.Reason}){(decision.CanSend ? "" : ", this build has no telemetry endpoint")}");
        Row("Logs", cli.Environment.LogsDirectory);

        cli.Console.Out.Write(new Panel(grid).Header("[bold]bit CLI[/]").Border(BoxBorder.Rounded).BorderStyle(new Style(Color.Grey)).Padding(1, 0, 1, 0));
        cli.Console.Out.MarkupLine("[grey]Everything bit installs or changes is listed before it runs, and each step prints the command it uses.[/]");
    }

    public static string OsName(HostOs os) => os switch
    {
        HostOs.Windows => "Windows",
        HostOs.MacOS => "macOS",
        _ => "Linux"
    };
}
