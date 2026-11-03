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
    internal static async Task RunAsync(BevMcpExports exports, Stream? input, Stream? output, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        var builder = services.AddMcpServer(o =>
        {
            o.ServerInfo = new Implementation { Name = "bevtools", Version = BevCliFormats.ToolVersion };
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

        builder.WithTools(new BevMcpTools(exports));
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

/// <summary>One export file, parsed once at startup and only read after that.</summary>
internal sealed class BevMcpExport(int index, string file, string json, JsonDocument document)
{
    public int Index { get; } = index;

    /// <summary>The path as it was given on the command line.</summary>
    public string File { get; } = file;

    /// <summary>The text, for <see cref="BevSessionCheck"/> and <see cref="BevProfileComparison"/>, which read text.</summary>
    public string Json { get; } = json;

    public JsonElement Root => document.RootElement;

    internal JsonDocument Document => document;

    /// <summary>A top-level array section's objects; null when the export has no such section.</summary>
    public List<JsonElement>? Array(string name) =>
        Root.TryGetProperty(name, out var section) && section.ValueKind == JsonValueKind.Array
            ? section.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object).ToList()
            : null;

    public JsonElement? Object(string name) =>
        Root.TryGetProperty(name, out var section) && section.ValueKind == JsonValueKind.Object ? section : null;
}

/// <summary>The exports a server was started with, and how a tool call names one of them.</summary>
internal sealed class BevMcpExports : IDisposable
{
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 256 };

    private readonly List<BevMcpExport> _exports;

    private BevMcpExports(List<BevMcpExport> exports) => _exports = exports;

    public IReadOnlyList<BevMcpExport> All => _exports;

    /// <summary>Parses every export before anything is served, so one that is not an export ends startup, not a call.</summary>
    /// <exception cref="FormatException">A text is not JSON, or not a BevTools session export.</exception>
    public static BevMcpExports Parse(IEnumerable<(string File, string Json)> files)
    {
        var exports = new List<BevMcpExport>();
        try
        {
            foreach (var (file, json) in files)
            {
                JsonDocument document;
                try
                {
                    document = JsonDocument.Parse(json, DocumentOptions);
                }
                catch (JsonException ex)
                {
                    throw new FormatException(file + ": not JSON: " + ex.Message, ex);
                }

                var export = new BevMcpExport(exports.Count, file, json, document);
                exports.Add(export);
                if (export.Root.ValueKind != JsonValueKind.Object || !export.Root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String
                    || format.GetString()?.StartsWith("bit-bevtools/", StringComparison.Ordinal) != true)
                {
                    throw new FormatException(file + ": not a BevTools session export: it has no \"format\": \"bit-bevtools/…\".");
                }
            }
        }
        catch
        {
            foreach (var export in exports)
            {
                export.Document.Dispose();
            }

            throw;
        }

        return new BevMcpExports(exports);
    }

    /// <summary>
    /// The export <paramref name="session"/> names: an index from <c>list_sessions</c>, the path as given, or its file
    /// name. May be left out only when one export is loaded.
    /// </summary>
    /// <exception cref="McpException">It names none, or more than one, of the loaded exports.</exception>
    public BevMcpExport Get(string? session, string parameter = "session")
    {
        if (string.IsNullOrWhiteSpace(session))
        {
            return _exports.Count == 1
                ? _exports[0]
                : throw new McpException($"{_exports.Count} exports are loaded: name one with {parameter} (an index or a file name). {Loaded()}");
        }

        session = session.Trim();
        if (int.TryParse(session, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
        {
            return index < _exports.Count ? _exports[index] : throw new McpException($"No export has index {index}. {Loaded()}");
        }

        var matches = _exports.Where(e => string.Equals(e.File, session, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
        {
            matches = _exports.Where(e => string.Equals(Path.GetFileName(e.File), session, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new McpException($"No loaded export is named '{BevCliFormats.Clip(session, 200) ?? session}'. {Loaded()}"),
            _ => throw new McpException($"Several loaded exports are named '{session}': name one by its index. {Loaded()}"),
        };
    }

    public void Dispose()
    {
        foreach (var export in _exports)
        {
            export.Document.Dispose();
        }
    }

    private string Loaded() => "Loaded: " + string.Join(", ", _exports.Select(e => e.Index.ToString(CultureInfo.InvariantCulture) + " = " + e.File)) + ".";
}
