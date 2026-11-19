using System.Text.Json;

namespace Bit.BevTools;

/// <summary>One export file, parsed once at startup and only read after that.</summary>
internal sealed class BevToolsMcpExport(int index, string file, string json, JsonDocument document)
{
    public int Index { get; } = index;

    /// <summary>The path as it was given on the command line.</summary>
    public string File { get; } = file;

    /// <summary>The text, for <see cref="BevToolsSessionCheck"/> and <see cref="BevToolsProfileComparison"/>, which read text.</summary>
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
