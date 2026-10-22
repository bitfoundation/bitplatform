using System.Globalization;
using System.Text.Json.Nodes;

namespace Bit.BlazorUI;

/// <summary>
/// A form the model asked for, which the user fills in and submits back.
/// </summary>
/// <remarks>
/// The point of an AI form is confirmation: rather than a model calling a tool with arguments it
/// guessed, it proposes a form, the user corrects it, and the corrected values are what execute.
/// </remarks>
public sealed record BitAiFormDefinition
{
    /// <summary>How deep nested objects and arrays are followed before the rest is flattened to text.</summary>
    private const int MaxDepth = 8;

    /// <summary>The fields, in display order.</summary>
    public IReadOnlyList<BitAiFormField> Fields { get; init; } = [];

    /// <summary>A heading above the form.</summary>
    public string? Title { get; init; }

    /// <summary>A line of explanation under the heading.</summary>
    public string? Description { get; init; }

    /// <summary>The label on the submit button. Defaults to <see cref="BitAiStrings.Submit"/>.</summary>
    public string? SubmitLabel { get; init; }

    /// <summary>
    /// The label on the cancel button. Defaults to <see cref="BitAiStrings.Cancel"/>; set it to an empty
    /// string to hide the button.
    /// </summary>
    /// <remarks>
    /// The defaults are left unset here rather than written in English, so that a form a model
    /// produces is labelled in whatever language the application's string table speaks.
    /// </remarks>
    public string? CancelLabel { get; init; }

    /// <summary>
    /// Builds a form from a JSON Schema object, so a model can describe one with the same vocabulary
    /// it already uses for tool arguments.
    /// </summary>
    /// <param name="schema">A JSON Schema object with a <c>properties</c> member.</param>
    /// <param name="values">Optional prefilled values, keyed by property name. Take precedence over schema defaults.</param>
    /// <returns>The form definition.</returns>
    /// <remarks>
    /// <para>
    /// Understands <c>type</c> (including <c>["string","null"]</c>, which makes the field optional),
    /// <c>format</c>, <c>enum</c> with <c>enumNames</c>, <c>oneOf</c>/<c>anyOf</c> of <c>const</c>
    /// values, <c>const</c>, <c>default</c>, <c>readOnly</c>, numeric and length bounds,
    /// <c>multipleOf</c>, nested <c>object</c> properties, <c>array</c> items, and local
    /// <c>$ref</c>s into <c>$defs</c> or <c>definitions</c>.
    /// </para>
    /// <para>
    /// Nothing here throws on a malformed schema: every keyword is read only when it has the kind the
    /// specification gives it, and anything else falls back to a text box, because the schema was
    /// written by a model and a form with a dumb field beats no form.
    /// </para>
    /// </remarks>
    public static BitAiFormDefinition FromJsonSchema(JsonElement schema, JsonElement? values = null)
    {
        if (schema.ValueKind is not JsonValueKind.Object) return new BitAiFormDefinition();

        var root = schema;
        var resolved = Resolve(schema, root);

        return new BitAiFormDefinition
        {
            Fields = BuildFields(resolved, root, values, 0),
            Title = GetString(resolved, "title"),
            Description = GetString(resolved, "description"),
        };
    }

    /// <summary>
    /// Builds a JSON value in the shape of the form from the flat values an <see cref="BitAiForm"/>
    /// collects: nested objects become objects, array entries become arrays, and numbers and booleans
    /// are encoded as such rather than as text.
    /// </summary>
    /// <param name="values">The values, keyed by field path (<c>address.city</c>, <c>tags[0]</c>).</param>
    /// <returns>A JSON object with one member per field that has a value.</returns>
    public JsonElement ToJson(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return JsonSerializer.SerializeToElement(BuildObject(Fields, null, values), BitAiJsonContext.Default.JsonObject);
    }

    /// <summary>Joins a parent path and a member name.</summary>
    /// <param name="parent">The parent path, or <see langword="null"/> at the root.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The member path.</returns>
    public static string Path(string? parent, string name) => parent is null ? name : $"{parent}.{name}";

    /// <summary>Builds the path of one array entry.</summary>
    /// <param name="arrayPath">The array field path.</param>
    /// <param name="index">The zero-based entry index.</param>
    /// <returns>The entry path.</returns>
    public static string IndexPath(string arrayPath, int index) =>
        string.Create(CultureInfo.InvariantCulture, $"{arrayPath}[{index}]");

    /// <summary>Counts the entries of an array field from the flat values, as the highest index seen plus one.</summary>
    /// <param name="values">The flat values.</param>
    /// <param name="arrayPath">The array field path.</param>
    /// <returns>The entry count.</returns>
    public static int CountItems(IReadOnlyDictionary<string, string?> values, string arrayPath)
    {
        var count = 0;
        var prefix = arrayPath + "[";
        foreach (var key in values.Keys)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var end = key.IndexOf(']', prefix.Length);
            if (end < 0) continue;
            if (int.TryParse(key.AsSpan(prefix.Length, end - prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                count = Math.Max(count, index + 1);
            }
        }

        return count;
    }

    private static JsonObject BuildObject(IReadOnlyList<BitAiFormField> fields, string? parent, IReadOnlyDictionary<string, string?> values)
    {
        var result = new JsonObject();
        foreach (var field in fields)
        {
            var node = BuildValue(field, Path(parent, field.Name), values);
            if (node is not null || field.Required) result[field.Name] = node;
        }

        return result;
    }

    private static JsonNode? BuildValue(BitAiFormField field, string path, IReadOnlyDictionary<string, string?> values)
    {
        switch (field.Type)
        {
            case BitAiFieldType.Object:
                var nested = BuildObject(field.Fields ?? [], path, values);
                return nested.Count > 0 || field.Required ? nested : null;

            case BitAiFieldType.Array:
                var array = new JsonArray();
                var count = CountItems(values, path);
                var template = field.Items ?? new BitAiFormField("item");
                for (var i = 0; i < count; i++)
                {
                    var entry = BuildValue(template, IndexPath(path, i), values);
                    if (entry is not null) array.Add(entry);
                }

                return array.Count > 0 || field.Required ? array : null;

            default:
                var text = values.GetValueOrDefault(path);
                if (string.IsNullOrWhiteSpace(text)) return null;
                return Encode(field, text);
        }
    }

    private static JsonNode? Encode(BitAiFormField field, string text)
    {
        var kind = field.JsonType ?? field.Type switch
        {
            BitAiFieldType.Boolean => "boolean",
            BitAiFieldType.Number => field.Integer ? "integer" : "number",
            _ => "string",
        };

        switch (kind)
        {
            case "boolean":
                return JsonValue.Create(string.Equals(text, "true", StringComparison.OrdinalIgnoreCase));

            case "integer":
                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole)
                    ? JsonValue.Create(whole)
                    : double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var rounded)
                        ? JsonValue.Create(rounded)
                        : JsonValue.Create(text);

            case "number":
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    ? JsonValue.Create(number)
                    : JsonValue.Create(text);

            case "object":
            case "array":
                // A free-form object or array the user edited as JSON text.
                try
                {
                    return JsonNode.Parse(text);
                }
                catch (JsonException)
                {
                    return JsonValue.Create(text);
                }

            default:
                return JsonValue.Create(text);
        }
    }

    private static List<BitAiFormField> BuildFields(JsonElement node, JsonElement root, JsonElement? values, int depth)
    {
        var fields = new List<BitAiFormField>();
        if (!TryGetObject(node, "properties", out var properties)) return fields;

        var required = new HashSet<string>(StringComparer.Ordinal);
        if (node.TryGetProperty("required", out var requiredList) && requiredList.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in requiredList.EnumerateArray())
            {
                if (item.ValueKind is JsonValueKind.String && item.GetString() is { } name) required.Add(name);
            }
        }

        foreach (var property in properties.EnumerateObject())
        {
            JsonElement? prefill = null;
            if (values is { ValueKind: JsonValueKind.Object } payload && payload.TryGetProperty(property.Name, out var given))
            {
                prefill = given;
            }

            fields.Add(FromSchemaProperty(property.Name, property.Value, root, required.Contains(property.Name), prefill, depth + 1));
        }

        return fields;
    }

    private static BitAiFormField FromSchemaProperty(string name, JsonElement original, JsonElement root, bool required, JsonElement? prefill, int depth)
    {
        var node = Resolve(original, root);

        // A $ref may carry its own title or description beside the reference; those win over the target's.
        string? Text(string key) => GetString(original, key) ?? GetString(node, key);

        var (type, nullable) = TypeOf(node);
        if (nullable) required = false;
        if (node.TryGetProperty("nullable", out var openApiNullable) && openApiNullable.ValueKind is JsonValueKind.True) required = false;

        // oneOf / anyOf: either an enumeration spelled as const alternatives, or a nullable wrapper.
        IReadOnlyList<string>? options = null;
        IReadOnlyList<string>? optionLabels = null;
        if (Alternatives(node) is { } alternatives)
        {
            if (TryConstOptions(alternatives, root, out options, out optionLabels, out var constType))
            {
                type ??= constType;
            }
            else
            {
                var variant = alternatives
                    .Select(a => Resolve(a, root))
                    .FirstOrDefault(a => TypeOf(a).Type is not null and not "null");
                if (variant.ValueKind is JsonValueKind.Object)
                {
                    if (alternatives.Any(a => TypeOf(Resolve(a, root)).Type is "null")) required = false;
                    node = variant;
                    (type, _) = TypeOf(variant);
                }
            }
        }

        if (node.TryGetProperty("enum", out var enumNode) && enumNode.ValueKind is JsonValueKind.Array)
        {
            var entries = enumNode.EnumerateArray().Where(e => e.ValueKind is not JsonValueKind.Null).ToArray();
            options = entries.Select(Stringify).ToArray();
            type ??= entries.Length > 0 ? KindToType(entries[0].ValueKind) : "string";

            var names = node.TryGetProperty("enumNames", out var enumNames) ? enumNames
                : node.TryGetProperty("x-enumNames", out var xEnumNames) ? xEnumNames
                : default;
            if (names.ValueKind is JsonValueKind.Array && names.GetArrayLength() == enumNode.GetArrayLength())
            {
                var labels = names.EnumerateArray().Select(Stringify).ToArray();
                optionLabels = enumNode.EnumerateArray()
                    .Select((e, i) => (e, i))
                    .Where(x => x.e.ValueKind is not JsonValueKind.Null)
                    .Select(x => labels[x.i])
                    .ToArray();
            }
        }

        var readOnly = node.TryGetProperty("readOnly", out var readOnlyNode) && readOnlyNode.ValueKind is JsonValueKind.True;
        string? value = null;

        if (node.TryGetProperty("const", out var constNode) && constNode.ValueKind is not JsonValueKind.Undefined)
        {
            value = Stringify(constNode);
            type ??= KindToType(constNode.ValueKind);
            readOnly = true;
        }

        type ??= TryGetObject(node, "properties", out _) ? "object"
            : node.TryGetProperty("items", out _) ? "array"
            : "string";

        var format = GetString(node, "format");
        var fieldType = (type, format, options) switch
        {
            (_, _, not null) => BitAiFieldType.Select,
            ("boolean", _, _) => BitAiFieldType.Boolean,
            ("integer" or "number", _, _) => BitAiFieldType.Number,
            ("object", _, _) when depth <= MaxDepth && TryGetObject(node, "properties", out _) => BitAiFieldType.Object,
            ("object", _, _) => BitAiFieldType.MultilineText,
            ("array", _, _) when depth <= MaxDepth => BitAiFieldType.Array,
            ("array", _, _) => BitAiFieldType.MultilineText,
            (_, "date", _) => BitAiFieldType.Date,
            (_, "date-time", _) => BitAiFieldType.DateTime,
            (_, "time", _) => BitAiFieldType.Time,
            (_, "email" or "idn-email", _) => BitAiFieldType.Email,
            (_, "uri" or "url" or "uri-reference" or "iri", _) => BitAiFieldType.Url,
            (_, "password", _) => BitAiFieldType.Password,
            (_, "textarea", _) => BitAiFieldType.MultilineText,
            _ => BitAiFieldType.Text,
        };

        // Prefill: an explicit value wins over the schema default; neither applies to a const.
        JsonElement? seed = prefill is { ValueKind: not (JsonValueKind.Undefined or JsonValueKind.Null) } ? prefill
            : node.TryGetProperty("default", out var defaultNode) && defaultNode.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null) ? defaultNode
            : null;

        if (value is null && seed is { } seedValue && fieldType is not BitAiFieldType.Object)
        {
            value = fieldType is BitAiFieldType.Array || type is "object" or "array" ? seedValue.GetRawText() : Stringify(seedValue);
        }

        IReadOnlyList<BitAiFormField>? fields = null;
        BitAiFormField? items = null;

        if (fieldType is BitAiFieldType.Object)
        {
            fields = BuildFields(node, root, seed, depth);
        }
        else if (fieldType is BitAiFieldType.Array)
        {
            var itemsNode = node.TryGetProperty("items", out var rawItems) ? rawItems : default;
            if (itemsNode.ValueKind is JsonValueKind.Array) itemsNode = itemsNode.GetArrayLength() > 0 ? itemsNode[0] : default;
            items = itemsNode.ValueKind is JsonValueKind.Object
                ? FromSchemaProperty("item", itemsNode, root, required: false, prefill: null, depth + 1)
                : new BitAiFormField("item");
        }

        var (min, max, exclusiveMin, exclusiveMax) = Bounds(node);

        return new BitAiFormField(name, Text("title"))
        {
            Type = fieldType,
            Description = Text("description"),
            Placeholder = GetString(node, "examples") ?? FirstExample(node),
            Options = options,
            OptionLabels = optionLabels,
            Required = required,
            ReadOnly = readOnly,
            Value = value,
            Minimum = min,
            Maximum = max,
            ExclusiveMinimum = exclusiveMin,
            ExclusiveMaximum = exclusiveMax,
            MultipleOf = GetNumber(node, "multipleOf") is { } step && step > 0 ? step : null,
            Integer = type is "integer",
            MinLength = GetInt(node, "minLength"),
            MaxLength = GetInt(node, "maxLength"),
            MinItems = GetInt(node, "minItems"),
            MaxItems = GetInt(node, "maxItems"),
            Pattern = GetString(node, "pattern"),
            Fields = fields,
            Items = items,
            JsonType = type is "string" or "integer" or "number" or "boolean" or "object" or "array" ? type : null,
        };
    }

    /// <summary>Reads <c>type</c>, which may be a string or a list; a list yields its first non-null entry.</summary>
    private static (string? Type, bool Nullable) TypeOf(JsonElement node)
    {
        if (node.ValueKind is not JsonValueKind.Object || !node.TryGetProperty("type", out var typeNode)) return (null, false);

        switch (typeNode.ValueKind)
        {
            case JsonValueKind.String:
                return (typeNode.GetString(), false);

            case JsonValueKind.Array:
                string? first = null;
                var nullable = false;
                foreach (var entry in typeNode.EnumerateArray())
                {
                    if (entry.ValueKind is not JsonValueKind.String) continue;
                    var text = entry.GetString();
                    if (text == "null") nullable = true;
                    else first ??= text;
                }

                return (first, nullable);

            default:
                return (null, false);
        }
    }

    private static JsonElement[]? Alternatives(JsonElement node)
    {
        foreach (var key in new[] { "oneOf", "anyOf" })
        {
            if (node.TryGetProperty(key, out var list) && list.ValueKind is JsonValueKind.Array && list.GetArrayLength() > 0)
            {
                return list.EnumerateArray().ToArray();
            }
        }

        return null;
    }

    /// <summary>Recognises <c>oneOf: [{const: "a", title: "A"}, ...]</c> as a select.</summary>
    private static bool TryConstOptions(
        JsonElement[] alternatives, JsonElement root,
        out IReadOnlyList<string>? options, out IReadOnlyList<string>? labels, out string? type)
    {
        var values = new List<string>();
        var titles = new List<string>();
        var anyTitle = false;
        type = null;

        foreach (var raw in alternatives)
        {
            var alternative = Resolve(raw, root);
            if (alternative.ValueKind is not JsonValueKind.Object) { options = labels = null; return false; }

            if (TypeOf(alternative).Type is "null") continue;

            JsonElement constant;
            if (alternative.TryGetProperty("const", out var constNode))
            {
                constant = constNode;
            }
            else if (alternative.TryGetProperty("enum", out var enumNode) && enumNode.ValueKind is JsonValueKind.Array && enumNode.GetArrayLength() == 1)
            {
                constant = enumNode[0];
            }
            else
            {
                options = labels = null;
                return false;
            }

            values.Add(Stringify(constant));
            type ??= KindToType(constant.ValueKind);
            var title = GetString(alternative, "title");
            anyTitle |= title is not null;
            titles.Add(title ?? Stringify(constant));
        }

        if (values.Count == 0) { options = labels = null; return false; }

        options = values;
        labels = anyTitle ? titles : null;
        return true;
    }

    private static (double? Min, double? Max, double? ExclusiveMin, double? ExclusiveMax) Bounds(JsonElement node)
    {
        var min = GetNumber(node, "minimum");
        var max = GetNumber(node, "maximum");
        double? exclusiveMin = null, exclusiveMax = null;

        if (node.TryGetProperty("exclusiveMinimum", out var exMin))
        {
            // Draft 4 spelled it as a flag on minimum; draft 6 onwards as a number of its own.
            if (exMin.ValueKind is JsonValueKind.Number && exMin.TryGetDouble(out var value)) exclusiveMin = value;
            else if (exMin.ValueKind is JsonValueKind.True && min is not null) { exclusiveMin = min; min = null; }
        }

        if (node.TryGetProperty("exclusiveMaximum", out var exMax))
        {
            if (exMax.ValueKind is JsonValueKind.Number && exMax.TryGetDouble(out var value)) exclusiveMax = value;
            else if (exMax.ValueKind is JsonValueKind.True && max is not null) { exclusiveMax = max; max = null; }
        }

        return (min, max, exclusiveMin, exclusiveMax);
    }

    /// <summary>
    /// Follows a local <c>$ref</c> (<c>#/$defs/...</c>, <c>#/definitions/...</c>, or any other JSON
    /// pointer into the document). Remote references and broken pointers leave the node as it is.
    /// </summary>
    private static JsonElement Resolve(JsonElement node, JsonElement root)
    {
        for (var hops = 0; hops < 16; hops++)
        {
            if (node.ValueKind is not JsonValueKind.Object
                || !node.TryGetProperty("$ref", out var refNode)
                || refNode.ValueKind is not JsonValueKind.String
                || refNode.GetString() is not { } pointer
                || !pointer.StartsWith('#'))
            {
                return node;
            }

            var current = root;
            var ok = true;
            foreach (var rawSegment in pointer.AsSpan(1).ToString().Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                var segment = Uri.UnescapeDataString(rawSegment).Replace("~1", "/").Replace("~0", "~");
                if (current.ValueKind is JsonValueKind.Object && current.TryGetProperty(segment, out var next))
                {
                    current = next;
                }
                else if (current.ValueKind is JsonValueKind.Array
                    && int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                    && index < current.GetArrayLength())
                {
                    current = current[index];
                }
                else
                {
                    ok = false;
                    break;
                }
            }

            if (!ok || current.ValueKind is not JsonValueKind.Object) return node;
            node = current;
        }

        return node;
    }

    private static bool TryGetObject(JsonElement node, string name, out JsonElement value)
    {
        if (node.ValueKind is JsonValueKind.Object && node.TryGetProperty(name, out value) && value.ValueKind is JsonValueKind.Object)
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string? GetString(JsonElement node, string name) =>
        node.ValueKind is JsonValueKind.Object && node.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? GetNumber(JsonElement node, string name) =>
        node.ValueKind is JsonValueKind.Object && node.TryGetProperty(name, out var value)
        && value.ValueKind is JsonValueKind.Number && value.TryGetDouble(out var number)
            ? number
            : null;

    private static int? GetInt(JsonElement node, string name) =>
        node.ValueKind is JsonValueKind.Object && node.TryGetProperty(name, out var value)
        && value.ValueKind is JsonValueKind.Number && value.TryGetInt32(out var number) && number >= 0
            ? number
            : null;

    private static string? FirstExample(JsonElement node) =>
        node.ValueKind is JsonValueKind.Object && node.TryGetProperty("examples", out var examples)
        && examples.ValueKind is JsonValueKind.Array && examples.GetArrayLength() > 0
        && examples[0].ValueKind is JsonValueKind.String
            ? examples[0].GetString()
            : null;

    private static string Stringify(JsonElement element) =>
        element.ValueKind is JsonValueKind.String ? element.GetString() ?? string.Empty : element.GetRawText();

    private static string? KindToType(JsonValueKind kind) => kind switch
    {
        JsonValueKind.String => "string",
        JsonValueKind.Number => "number",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        _ => null,
    };
}
