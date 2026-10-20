using System.Text;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Bit.Belemetry.Demo.Server.Services;

namespace Bit.Belemetry.Demo.Server.Controllers;

/// <summary>
/// The documents behind the tools, for clients that pin context rather than call tools: the README, the
/// reference tables, and any embedded source file - the same text the tools hand out.
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "belemetry://docs/readme", Name = "bit Belemetry guide", MimeType = "text/markdown")]
    [Description("The README of Bit.Belemetry: install, setup per host, exporters, consent, instrumentation, privacy, processors, persistence, transport, conventions and troubleshooting.")]
    public static string Readme()
    {
        var readme = BelemetrySourceCatalog.Readme;

        return readme.Length == 0 ? "The README is not embedded in this build." : readme;
    }

    [McpServerResource(UriTemplate = "belemetry://docs/conventions", Name = "Reference tables", MimeType = "text/markdown")]
    [Description("Every reference table - feature bits, storage keys, semantic conventions, Azure Monitor mapping, environment variables, browser options - and the BelemetrySemanticConventions constants.")]
    public static string Conventions()
    {
        var builder = new StringBuilder("# bit Belemetry reference tables\n\n");

        foreach (var table in BelemetryConventions.Tables)
        {
            builder.AppendLine($"## {table.Section} ({table.Source})").AppendLine();
            builder.AppendLine("| " + string.Join(" | ", table.Headers) + " |");
            builder.AppendLine("|" + string.Concat(Enumerable.Repeat(" --- |", table.Headers.Length)));
            foreach (var row in table.Rows) builder.AppendLine("| " + string.Join(" | ", row) + " |");
            builder.AppendLine();
        }

        builder.AppendLine("## BelemetrySemanticConventions").AppendLine();
        foreach (var (name, value) in BelemetryConventions.Constants) builder.AppendLine($"- `{name}` = `{value}`");

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "belemetry://source/{path}", Name = "Embedded source file", MimeType = "text/plain")]
    [Description("An embedded file of the demo or the samples, e.g. belemetry://source/Demo%2FClient%2FShared%2FConsentBanner.razor (the path URL-encoded).")]
    public static string Source(string path)
    {
        return BelemetrySourceCatalog.GetSource(Uri.UnescapeDataString(path))
            ?? $"No embedded file '{path}'. Call GetBelemetrySourceFile with an empty path for the list.";
    }
}
