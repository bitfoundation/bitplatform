using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.Telemetry;

public static class TelemetryNotice
{
    public const int Version = 1;

    public const string DocsUrl = $"{BuildInfo.RepositoryUrl}/tree/develop/src/Cli#telemetry";

    public static bool ShouldShow(TelemetryDecision decision, CliSettings settings)
    {
        return decision.IsActive && decision.LogOnly is false && settings.TelemetryNoticeVersion < Version;
    }

    public static void Show(IAnsiConsole console, CliSettings settings)
    {
        console.MarkupLine("[bold]Telemetry[/]  [grey]bit sends error reports to the bit platform team so failed runs get fixed:[/]");
        console.MarkupLine("           [grey]which step failed and why, never names, paths or file contents.[/]");
        console.MarkupLine($"           [grey]Turn it off:[/] bit telemetry off [grey]· {Markup.Escape(DocsUrl)}[/]");
        console.WriteLine();

        settings.TelemetryNoticeVersion = Version;
        settings.Save();
    }
}
