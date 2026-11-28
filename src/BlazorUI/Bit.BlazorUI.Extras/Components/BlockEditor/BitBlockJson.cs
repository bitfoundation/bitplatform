using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The JSON form of a block document: <c>{ "v": 1, "blocks": [ { "id", "type", "text", "props", "children" } ] }</c>.
/// </summary>
/// <remarks>
/// It is what an app stores, so it changes by addition only: a new member, never a renamed or removed one. A reader
/// skips the members it does not know and keeps the blocks whose type it does not know, and a prop written as a number
/// or a boolean reads as its text, so JSON written by hand need not quote every value.
/// </remarks>
internal static class BitBlockJson
{
    public const int Version = 1;

    public static string Write(BitBlockDocument document)
    {
        var json = new BitBlockJsonDocument { V = Version, Blocks = [.. document.Blocks.Select(ToJson)] };
        try
        {
            return JsonSerializer.Serialize(json, BitBlockJsonContext.Default.BitBlockJsonDocument);
        }
        catch (InvalidOperationException e)
        {
            // The writer reports a document past the depth limit this way, and a block's props past it as a
            // JsonException; both are the same limit, so both are reported as the reader reports it.
            throw new JsonException($"The document is nested deeper than the JSON form's {BitBlockJsonContext.Default.Options.MaxDepth} levels.", e);
        }
    }

    public static IReadOnlyList<BitBlock> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        var document = JsonSerializer.Deserialize(json, BitBlockJsonContext.Default.BitBlockJsonDocument);
        return document?.Blocks is { } blocks ? [.. blocks.Where(b => b is not null).Select(FromJson)] : [];
    }

    private static BitBlockJsonBlock ToJson(BitBlock block)
        => new()
        {
            Id = block.Id,
            Type = block.Type,
            Text = block.Text.Length == 0 ? null : block.Text,
            Props = block.Props.Count == 0
                ? null
                : block.Props.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => JsonSerializer.SerializeToElement(p.Value, BitBlockJsonContext.Default.String)),
            Children = block.Children.Count == 0 ? null : [.. block.Children.Select(ToJson)]
        };

    private static BitBlock FromJson(BitBlockJsonBlock block)
    {
        var props = new Dictionary<string, string>(StringComparer.Ordinal);
        if (block.Props is not null)
        {
            foreach (var (name, value) in block.Props)
            {
                var text = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    _ => value.GetRawText()
                };
                if (text is not null) props[name] = text;
            }
        }

        return new BitBlock
        {
            // A block without an id is left without one: the document gives it one once every id the JSON does give is
            // known, so the new id can be none of them.
            Id = block.Id ?? "",
            Type = string.IsNullOrEmpty(block.Type) ? BitBlockTypes.Paragraph.Name : block.Type,
            Text = block.Text ?? "",
            Props = props,
            Children = block.Children is { } children ? [.. children.Where(c => c is not null).Select(FromJson)] : []
        };
    }
}

internal sealed class BitBlockJsonDocument
{
    [JsonPropertyName("v")]
    public int V { get; set; }

    [JsonPropertyName("blocks")]
    public List<BitBlockJsonBlock>? Blocks { get; set; }
}

internal sealed class BitBlockJsonBlock
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("props")]
    public Dictionary<string, JsonElement>? Props { get; set; }

    [JsonPropertyName("children")]
    public List<BitBlockJsonBlock>? Children { get; set; }
}

/// <summary>
/// The JSON metadata of the block document, generated at compile time so a trimmed or AOT-compiled app keeps every
/// member the JSON is read and written through. Nested lists go deep, so the depth limit is well past the default.
/// </summary>
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, MaxDepth = 512)]
[JsonSerializable(typeof(BitBlockJsonDocument))]
[JsonSerializable(typeof(string))]
internal sealed partial class BitBlockJsonContext : JsonSerializerContext;
