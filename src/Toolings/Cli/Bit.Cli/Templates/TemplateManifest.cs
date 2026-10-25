using System.Text.Json;

namespace Bit.Cli.Templates;

public enum TemplateParameterType
{
    Choice,
    Bool,
    String
}

public sealed record TemplateChoice(string Value, string? Description);

public sealed record TemplateParameter
{
    public required string Name { get; init; }

    public required TemplateParameterType Type { get; init; }

    public required string DefaultValue { get; init; }

    public IReadOnlyList<TemplateChoice> Choices { get; init; } = [];

    public string? DisplayName { get; init; }

    public string? Description { get; init; }

    public string? Normalize(string value)
    {
        return Type switch
        {
            TemplateParameterType.Choice => Choices.FirstOrDefault(c => string.Equals(c.Value, value, StringComparison.OrdinalIgnoreCase))?.Value,
            TemplateParameterType.Bool => value.Trim().ToLowerInvariant() switch
            {
                "" or "true" or "yes" or "1" => "true",
                "false" or "no" or "0" => "false",
                _ => null
            },
            _ => value
        };
    }
}

public sealed class TemplateManifest
{
    private static readonly string[] hiddenParameters = ["helpUrl"];

    private static readonly JsonDocumentOptions documentOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public required string Identity { get; init; }

    public required string ShortName { get; init; }

    public required string SourceName { get; init; }

    public required IReadOnlyList<TemplateParameter> Parameters { get; init; }

    public TemplateParameter? Find(string name)
    {
        return Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static TemplateManifest Embedded { get; } = LoadEmbedded();

    public static TemplateManifest Parse(string json)
    {
        using var document = JsonDocument.Parse(json, documentOptions);
        var root = document.RootElement;

        var parameters = new List<TemplateParameter>();

        foreach (var symbol in root.GetProperty("symbols").EnumerateObject())
        {
            var value = symbol.Value;

            if (GetString(value, "type") is not "parameter" || hiddenParameters.Contains(symbol.Name))
                continue;

            var type = GetString(value, "datatype")?.ToLowerInvariant() switch
            {
                "choice" => TemplateParameterType.Choice,
                "bool" or "boolean" => TemplateParameterType.Bool,
                _ => TemplateParameterType.String
            };

            var choices = value.TryGetProperty("choices", out var choicesElement) && choicesElement.ValueKind is JsonValueKind.Array
                ? choicesElement.EnumerateArray().Select(c => new TemplateChoice(GetString(c, "choice") ?? "", GetString(c, "description"))).Where(c => c.Value.Length > 0).ToArray()
                : [];

            parameters.Add(new TemplateParameter
            {
                Name = symbol.Name,
                Type = type,
                DefaultValue = GetString(value, "defaultValue") ?? (type is TemplateParameterType.Bool ? "false" : ""),
                Choices = choices,
                DisplayName = GetString(value, "displayName"),
                Description = GetString(value, "description")
            });
        }

        return new TemplateManifest
        {
            Identity = GetString(root, "identity") ?? "Bit.Boilerplate",
            ShortName = GetString(root, "shortName") ?? "bit-bp",
            SourceName = GetString(root, "sourceName") ?? "Boilerplate",
            Parameters = parameters
        };
    }

    private static TemplateManifest LoadEmbedded()
    {
        using var stream = typeof(TemplateManifest).Assembly.GetManifestResourceStream("Bit.Cli.template.json")
            ?? throw new InvalidOperationException("The bit Boilerplate's template.json isn't embedded in this build.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) ? property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        } : null;
    }
}
