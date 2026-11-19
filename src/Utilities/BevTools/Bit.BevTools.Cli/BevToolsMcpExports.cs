using System.Globalization;
using System.Text.Json;
using ModelContextProtocol;

namespace Bit.BevTools;

/// <summary>The exports a server was started with, and how a tool call names one of them.</summary>
internal sealed class BevToolsMcpExports : IDisposable
{
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 256 };

    private readonly List<BevToolsMcpExport> _exports;

    private BevToolsMcpExports(List<BevToolsMcpExport> exports) => _exports = exports;

    public IReadOnlyList<BevToolsMcpExport> All => _exports;

    /// <summary>Parses every export before anything is served, so one that is not an export ends startup, not a call.</summary>
    /// <exception cref="FormatException">A text is not JSON, or not a BevTools session export.</exception>
    public static BevToolsMcpExports Parse(IEnumerable<(string File, string Json)> files)
    {
        var exports = new List<BevToolsMcpExport>();
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

                var export = new BevToolsMcpExport(exports.Count, file, json, document);
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

        return new BevToolsMcpExports(exports);
    }

    /// <summary>
    /// The export <paramref name="session"/> names: an index from <c>list_sessions</c>, the path as given, or its file
    /// name. May be left out only when one export is loaded.
    /// </summary>
    /// <exception cref="McpException">It names none, or more than one, of the loaded exports.</exception>
    public BevToolsMcpExport Get(string? session, string parameter = "session")
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

        // The name exactly as given first, so two exports that differ only in case can each be named; then regardless of case.
        List<BevToolsMcpExport> matches = [];
        foreach (var comparison in (StringComparison[])[StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase])
        {
            matches = _exports.Where(e => string.Equals(e.File, session, comparison)).ToList();
            if (matches.Count == 0)
            {
                matches = _exports.Where(e => string.Equals(Path.GetFileName(e.File), session, comparison)).ToList();
            }

            if (matches.Count > 0)
            {
                break;
            }
        }

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new McpException($"No loaded export is named '{BevToolsCliFormats.Clip(session, 200) ?? session}'. {Loaded()}"),
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
