using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.Cli.Templates;

public enum TemplateParameterType
{
    Choice,
    Bool,
    String
}

public sealed record TemplateChoice(string Value, string? Description);

public sealed record TemplateSwitchCase(string? Parameter, string? Value, string Result);

public sealed record TemplateSwitch(string Replaces, IReadOnlyList<TemplateSwitchCase> Cases);

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

public sealed partial class TemplateManifest
{
    private static readonly string[] hiddenParameters = ["helpUrl"];

    private static readonly JsonDocumentOptions documentOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public required string Identity { get; init; }

    public required string ShortName { get; init; }

    public required string SourceName { get; init; }

    public required IReadOnlyList<TemplateParameter> Parameters { get; init; }

    public IReadOnlyList<TemplateSwitch> Switches { get; init; } = [];

    public TemplateParameter? Find(string name)
    {
        return Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public string ApplySwitches(string text, TemplateSelection selection)
    {
        foreach (var templateSwitch in Switches)
        {
            var match = templateSwitch.Cases.FirstOrDefault(c => c.Parameter is null || string.Equals(selection[c.Parameter], c.Value, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                text = text.Replace(templateSwitch.Replaces, match.Result, StringComparison.Ordinal);
            }
        }

        return text;
    }

    public static TemplateManifest Embedded { get; } = LoadEmbedded();

    public static TemplateManifest Parse(string json)
    {
        using var document = JsonDocument.Parse(json, documentOptions);
        var root = document.RootElement;

        var parameters = new List<TemplateParameter>();
        var switches = new List<TemplateSwitch>();

        foreach (var symbol in root.GetProperty("symbols").EnumerateObject())
        {
            var value = symbol.Value;

            if (GetString(value, "type") is "generated" && GetString(value, "generator") is "switch")
            {
                if (ParseSwitch(value) is { } templateSwitch)
                {
                    switches.Add(templateSwitch);
                }

                continue;
            }

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
            Parameters = parameters,
            Switches = switches
        };
    }

    private static TemplateSwitch? ParseSwitch(JsonElement symbol)
    {
        if (GetString(symbol, "replaces") is not { Length: > 0 } replaces
            || symbol.TryGetProperty("parameters", out var parameters) is false
            || parameters.TryGetProperty("cases", out var cases) is false
            || cases.ValueKind is not JsonValueKind.Array)
            return null;

        var parsed = new List<TemplateSwitchCase>();

        foreach (var templateCase in cases.EnumerateArray())
        {
            if (GetString(templateCase, "value") is not { } result)
                continue;

            var condition = GetString(templateCase, "condition") ?? "";

            if (condition.Trim().Length is 0)
            {
                parsed.Add(new(null, null, result));
            }
            else if (SwitchConditionRegex().Match(condition) is { Success: true } match)
            {
                parsed.Add(new(match.Groups["parameter"].Value, match.Groups["value"].Value, result));
            }
        }

        return new TemplateSwitch(replaces, parsed);
    }

    [GeneratedRegex("""^\s*\(\s*(?<parameter>\w+)\s*==\s*['"](?<value>[^'"]*)['"]\s*\)\s*$""")]
    private static partial Regex SwitchConditionRegex();

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
