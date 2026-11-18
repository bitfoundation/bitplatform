using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Bit.BevTools;

/// <summary>
/// <c>bevtools mcp</c>: a Model Context Protocol server over stdio that lets a coding agent investigate session exports.
/// It serves the files it was started with and nothing else: no running app (that needs an authenticated transport
/// the page hub deliberately does not offer), no other file, no writes. Exports are redacted by BevTools at capture,
/// and every answer is taken from them as they are, bounded so that one call cannot flood an agent's context.
/// </summary>
internal static class BevToolsMcp
{
    internal const string Instructions =
        "BevTools session exports, read-only. This server serves only the export files it was started with "
        + "(bevtools mcp <export.json>...): it never connects to a running app, never reads any other file and never "
        + "writes one. An export is what BevTools recorded in a Blazor app - findings of its rules, errors, components and "
        + "renders, HTTP requests, the timeline - with values redacted at capture (credential-shaped values, sensitive "
        + "names, [BevToolsSensitive] members); answers come from the files as they are, so what is redacted or missing "
        + "in a file is redacted or missing here. Start with list_sessions, then check or get_findings for what the rules "
        + "reported, and follow their evidence: componentInstanceId to get_component, relatedEventIds to get_timeline, "
        + "errors to get_errors and get_error_groups. With several exports loaded, name one with session (its index or "
        + "file name). Every answer is bounded: lists give total, returned and truncated; a string cut short ends in an "
        + "ellipsis and is named in truncatedFields.";

    /// <summary>
    /// Serves <paramref name="exports"/> until the client closes the connection: over stdio when
    /// <paramref name="input"/> and <paramref name="output"/> are null, over those streams otherwise (tests).
    /// </summary>
    internal static async Task RunAsync(BevToolsMcpExports exports, Stream? input, Stream? output, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        var builder = services.AddMcpServer(o =>
        {
            o.ServerInfo = new Implementation { Name = "bevtools", Version = BevToolsCliFormats.ToolVersion };
            o.ServerInstructions = Instructions;
        });
        if (input is null || output is null)
        {
            builder.WithStdioServerTransport();
        }
        else
        {
            builder.WithStreamServerTransport(input, output);
        }

        builder.WithTools(new BevToolsMcpTools(exports));
        // One call at a time: a client may send several at once, and the exports are JsonDocuments, which are not
        // documented as safe to read from several threads. Every call is a bounded read of memory, so nothing waits long.
        using var gate = new SemaphoreSlim(1, 1);
        builder.WithRequestFilters(filters => filters.AddCallToolFilter(next => async (request, cancellationToken) =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await next(request, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }));
        // No host: the server is the process's only job, and a host would add console logging, which on stdout would
        // corrupt the protocol stream.
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<McpServer>().RunAsync(cancellationToken);
    }
}
