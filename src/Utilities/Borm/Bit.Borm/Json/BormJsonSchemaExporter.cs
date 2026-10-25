using System.Globalization;
using System.Text.Json;

namespace Bit.Borm;

/// <summary>
/// Serialises a <see cref="BormDefinition"/> to a JSON Schema document (with <c>x-</c> extensions for
/// UI intent), enabling forms to be stored, transmitted and re-imported via <see cref="BormJsonSchemaImporter"/>.
/// </summary>
/// <remarks>
/// The export is designed to round-trip: conditions, wizard steps, layout and widget choices all
/// survive, and so do expressions — conditions, computed values (<c>x-computed</c>) and rules
/// (<c>x-rules</c>) written as <see cref="BormExpression"/>s. The one thing that cannot is a
/// condition or rule backed by a delegate (<see cref="BormPredicateCondition"/>,
/// <c>Must(predicate)</c>/<c>MustAsync</c>) — code has no JSON form. Such conditions are omitted
/// rather than approximated.
/// </remarks>
public static class BormJsonSchemaExporter
{
    private const string SchemaVersion = "https://json-schema.org/draft/2020-12/schema";

    public static string Export(BormDefinition form, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(form);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
            WriteRoot(writer, form);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteRoot(Utf8JsonWriter writer, BormDefinition form)
    {
        writer.WriteStartObject();
        writer.WriteString("$schema", SchemaVersion);
        WriteObjectBody(writer, form.Title, form.Description, form.Fields);

        if (form.Columns > 1) writer.WriteNumber("x-columns", form.Columns);
        if (form.Version is not null) writer.WriteString("x-version", form.Version);
        if (form.HasSteps && form.StepLayout != BormStepLayout.Wizard)
            writer.WriteString("x-stepLayout", form.StepLayout.ToString().ToLowerInvariant());
        WriteSteps(writer, form);
        WriteRules(writer, form.Validators, formLevel: true);

        if (form.Translations.Count > 0)
        {
            writer.WritePropertyName("x-translations");
            writer.WriteStartObject();
            foreach (var (culture, table) in form.Translations)
            {
                writer.WritePropertyName(culture);
                writer.WriteStartObject();
                foreach (var (key, text) in table) writer.WriteString(key, text);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteObjectSchema(
        Utf8JsonWriter writer, string? title, string? description, IEnumerable<BormFieldDefinition> fields)
    {
        writer.WriteStartObject();
        WriteObjectBody(writer, title, description, fields);
        writer.WriteEndObject();
    }

    /// <summary>Writes the members of an object schema without opening or closing the object itself.</summary>
    private static void WriteObjectBody(
        Utf8JsonWriter writer, string? title, string? description, IEnumerable<BormFieldDefinition> fields)
    {
        writer.WriteString("type", "object");
        if (title is not null) writer.WriteString("title", title);
        if (description is not null) writer.WriteString("description", description);

        var fieldList = fields.ToList();

        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        foreach (var field in fieldList)
        {
            writer.WritePropertyName(field.Name);
            WriteField(writer, field);
        }
        writer.WriteEndObject();

        var required = fieldList.Where(f => f.Required).Select(f => f.Name).ToList();
        if (required.Count > 0)
        {
            writer.WritePropertyName("required");
            writer.WriteStartArray();
            foreach (var r in required) writer.WriteStringValue(r);
            writer.WriteEndArray();
        }
    }

    private static void WriteField(Utf8JsonWriter writer, BormFieldDefinition field)
    {
        writer.WriteStartObject();

        if (field.Type == BormFieldType.Object)
        {
            // An object field is an object schema plus the same UI metadata every other field carries.
            WriteObjectBody(writer, field.Label, field.HelpText, field.Children);
            if (field.ReadOnly) writer.WriteBoolean("readOnly", true);
            WriteUiMetadata(writer, field);
            writer.WriteEndObject();
            return;
        }

        var (jsonType, format, widget) = MapType(field.Type);

        // A choice stores one of its option values, so the field's JSON type is the type of those
        // values. An integer enum exported as a string enum came back as text, and the exported
        // `default` — still a number — was not even one of its own enum's members.
        //
        // Only when every option really is written as that type, though. A decimal select offering
        // "0.50" and "1.50" keeps those spellings as text (writing 1.5 would change the option), and
        // `"type": "number"` over an enum of strings is a schema that contradicts itself.
        var valueType = ValueJsonType(field.ValueType);
        if (valueType is not null && !field.Options.All(o => IsTypedToken(o.Value, valueType)))
            valueType = null;
        if (field.Type is BormFieldType.Select or BormFieldType.Radio or BormFieldType.Combobox
            && valueType is not null)
            jsonType = valueType;

        var hasUuidRule = field.Validators.Any(v => v is BormUuidRule);
        if (format is null && hasUuidRule) format = "uuid";

        writer.WriteString("type", jsonType);
        if (format is not null) writer.WriteString("format", format);

        if (field.Label is not null) writer.WriteString("title", field.Label);
        if (field.HelpText is not null) writer.WriteString("description", field.HelpText);
        if (widget is not null) writer.WriteString("x-widget", widget);
        if (field.ReadOnly) writer.WriteBoolean("readOnly", true);

        // A tag list's length limits and pattern are about each tag, and JSON Schema applies
        // `minLength`, `maxLength` and `pattern` to strings only — on the array they constrain nothing
        // any other validator will check. They go on `items`, below.
        var isTags = field.Type == BormFieldType.Tags;
        if (!isTags)
        {
            if (field.MinLength is { } minL) writer.WriteNumber("minLength", minL);
            if (field.MaxLength is { } maxL) writer.WriteNumber("maxLength", maxL);
        }

        WriteBounds(writer, field);

        // A UI step and a `multipleOf` constraint are different things: a price stepping by 0.01 in the
        // spinner is not a promise that 0.005 is invalid. The constraint comes from the rule itself —
        // the step is only where the spinner starts — and the control's granularity travels as x-step
        // whenever it says something the constraint does not.
        var multiple = field.Validators.OfType<BormMultipleOfRule>().FirstOrDefault();
        if (multiple is not null && IsFinite(multiple.Factor) && multiple.Factor > 0)
            writer.WriteNumber("multipleOf", multiple.Factor);
        if (field.NumericStep is { } step && IsFinite(step) && (multiple is null || step != multiple.Factor))
            writer.WriteNumber("x-step", step);

        // The UUID pattern is only the browser hint that stands in for `format: uuid`, written above.
        if (!isTags && field.Pattern is { } pattern
            && !(hasUuidRule && pattern == BormJsonSchemaImporter.UuidPattern))
            writer.WriteString("pattern", pattern);
        if (!isTags) WriteAdditionalPatterns(writer, field);
        if (field.MinItems is { } minI) writer.WriteNumber("minItems", minI);
        if (field.MaxItems is { } maxI) writer.WriteNumber("maxItems", maxI);

        // A multi-select is a set: it cannot hold the same answer twice, and the standard spelling of
        // "pick several of these" says so.
        if (field.Type == BormFieldType.MultiSelect || field.Validators.Any(v => v.Key == "uniqueItems"))
            writer.WriteBoolean("uniqueItems", true);

        if (field.Options.Count > 0 && field.Type != BormFieldType.MultiSelect)
        {
            // A combobox that also accepts answers on no list must not say `enum`, which means "one of
            // these and nothing else" to every validator that reads it. Its choices travel as x-enum.
            var open = field.Type == BormFieldType.Combobox && !field.Validators.Any(v => v.Key == "oneOf");
            if (open) writer.WriteBoolean("x-allowCustom", true);
            WriteOptions(writer, field.Options, jsonType, open ? "x-enum" : "enum");
        }

        if (field.DefaultValue is not null && BormConditionJson.IsWritable(field.DefaultValue))
        {
            writer.WritePropertyName("default");
            BormConditionJson.WriteValue(writer, field.DefaultValue, field.Type);
        }

        if (field.Type == BormFieldType.Array && field.ItemTemplate is not null)
        {
            writer.WritePropertyName("items");
            if (field.ItemTemplate.Type == BormFieldType.Object)
                WriteObjectSchema(writer, null, null, field.ItemTemplate.Children);
            else
                WriteField(writer, field.ItemTemplate);
        }
        else if (field.Type == BormFieldType.MultiSelect && (field.Options.Count > 0 || valueType is not null))
        {
            // `enum` beside `"type": "array"` describes the array itself, so no list of answers could
            // ever satisfy it. The choices belong to each item — and so does their type, which a
            // multi-select fed by a provider still has even with no choices to write down.
            writer.WritePropertyName("items");
            writer.WriteStartObject();
            var itemType = valueType ?? "string";
            writer.WriteString("type", itemType);
            if (field.Options.Count > 0) WriteOptions(writer, field.Options, itemType, "enum");
            writer.WriteEndObject();
        }
        else if (isTags)
        {
            WriteTagItems(writer, field);
        }

        WriteUiMetadata(writer, field, writeExamples: !isTags);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes the numeric bounds, exclusive ones as <c>exclusiveMinimum</c>/<c>exclusiveMaximum</c>. A
    /// bound that is not a finite number has no JSON form — <c>Range(0, double.PositiveInfinity)</c>
    /// means "no upper bound" — and writing one threw.
    /// </summary>
    private static void WriteBounds(Utf8JsonWriter writer, BormFieldDefinition field)
    {
        var range = field.Validators.OfType<BormRangeRule>().FirstOrDefault();

        if (field.Min is { } min && IsFinite(min))
            writer.WriteNumber(range is { MinExclusive: true } && range.Min == min ? "exclusiveMinimum" : "minimum", min);
        if (field.Max is { } max && IsFinite(max))
            writer.WriteNumber(range is { MaxExclusive: true } && range.Max == max ? "exclusiveMaximum" : "maximum", max);
    }

    /// <summary>The per-tag constraints and suggestions of a tag list, written where JSON Schema applies them.</summary>
    private static void WriteTagItems(Utf8JsonWriter writer, BormFieldDefinition field)
    {
        var hasPattern = field.Pattern is not null || field.Validators.Any(v => v is BormAdditionalPatternRule);
        if (field.MinLength is null && field.MaxLength is null && !hasPattern && field.Suggestions.Count == 0) return;

        writer.WritePropertyName("items");
        writer.WriteStartObject();
        writer.WriteString("type", "string");
        if (field.MinLength is { } minL) writer.WriteNumber("minLength", minL);
        if (field.MaxLength is { } maxL) writer.WriteNumber("maxLength", maxL);
        if (field.Pattern is { } pattern) writer.WriteString("pattern", pattern);
        WriteAdditionalPatterns(writer, field);
        if (field.Suggestions.Count > 0)
        {
            writer.WritePropertyName("examples");
            writer.WriteStartArray();
            foreach (var s in field.Suggestions) writer.WriteStringValue(s);
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes the patterns a value must match beyond the field's own — what an imported
    /// <c>allOf</c> of two patterns became. JSON Schema has one <c>pattern</c> slot per schema, so the
    /// others go in an <c>allOf</c>, where every validator enforces them and the importer merges them
    /// back into the same field.
    /// </summary>
    private static void WriteAdditionalPatterns(Utf8JsonWriter writer, BormFieldDefinition field)
    {
        var more = field.Validators.OfType<BormAdditionalPatternRule>().Select(r => r.Pattern).Distinct().ToList();
        if (more.Count == 0) return;

        writer.WritePropertyName("allOf");
        writer.WriteStartArray();
        foreach (var pattern in more)
        {
            writer.WriteStartObject();
            writer.WriteString("pattern", pattern);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    /// <summary>Writes a list of choices: their values under <paramref name="keyword"/>, then labels, groups and disabled flags.</summary>
    private static void WriteOptions(
        Utf8JsonWriter writer, IList<BormSelectOption> options, string jsonType, string keyword)
    {
        writer.WritePropertyName(keyword);
        writer.WriteStartArray();
        foreach (var o in options) WriteOptionValue(writer, o.Value, jsonType);
        writer.WriteEndArray();

        writer.WritePropertyName("enumNames");
        writer.WriteStartArray();
        foreach (var o in options) writer.WriteStringValue(o.Label);
        writer.WriteEndArray();

        // An option's group (the <optgroup> it sits in) and its disabled flag have no JSON Schema
        // vocabulary, so they travel alongside enumNames rather than being dropped on export — a
        // grouped country list that came back flat would be a worse form than the one exported.
        if (options.Any(o => o.Group is not null))
        {
            writer.WritePropertyName("x-enumGroups");
            writer.WriteStartArray();
            foreach (var o in options) writer.WriteStringValue(o.Group);
            writer.WriteEndArray();
        }

        if (options.Any(o => o.Disabled))
        {
            writer.WritePropertyName("x-enumDisabled");
            writer.WriteStartArray();
            foreach (var o in options.Where(o => o.Disabled)) WriteOptionValue(writer, o.Value, jsonType);
            writer.WriteEndArray();
        }
    }

    /// <summary>Writes the <c>x-</c> extensions carrying intent JSON Schema itself cannot express.</summary>
    private static void WriteUiMetadata(Utf8JsonWriter writer, BormFieldDefinition field, bool writeExamples = true)
    {
        if (field.Placeholder is not null) writer.WriteString("x-placeholder", field.Placeholder);
        if (field.Group is not null) writer.WriteString("x-group", field.Group);
        if (field.Order != 0) writer.WriteNumber("x-order", field.Order);
        if (field.Autocomplete is not null) writer.WriteString("x-autocomplete", field.Autocomplete);
        if (field.InputMode is not null) writer.WriteString("x-inputMode", field.InputMode);
        if (field.ColumnSpan is { } span) writer.WriteNumber("x-colSpan", span);
        if (field.Accept is not null) writer.WriteString("x-accept", field.Accept);
        if (field.Multiple) writer.WriteBoolean("x-multiple", true);
        if (field.ClearOnHide) writer.WriteBoolean("x-clearOnHide", true);
        if (field.Sensitive) writer.WriteBoolean("x-sensitive", true);
        if (field.TextKey is { Length: > 0 } textKey) writer.WriteString("x-i18n", textKey);
        if (field.Autofocus) writer.WriteBoolean("x-autofocus", true);
        if (field.MaxFileSize is { } maxFile) writer.WriteNumber("x-maxFileSize", maxFile);

        if (!field.ShowLabel) writer.WriteBoolean("x-showLabel", false);
        if (field.Prefix is not null) writer.WriteString("x-prefix", field.Prefix);
        if (field.Suffix is not null) writer.WriteString("x-suffix", field.Suffix);
        if (field.ShowCharacterCount) writer.WriteBoolean("x-characterCount", true);
        if (field.UpdateOn == BormUpdateTrigger.Input) writer.WriteString("x-updateOn", "input");
        if (field.DebounceMilliseconds > 0) writer.WriteNumber("x-debounce", field.DebounceMilliseconds);
        if (field.CustomRenderer is { Length: > 0 } renderer) writer.WriteString("x-renderer", renderer);

        // The other half of a cross-field rule. The rule itself may have no JSON form (a delegate), but
        // "revalidate me when that changes" always does, and losing it on a round trip would put back
        // the stale-message bug the declaration exists to prevent.
        if (field.RevalidateOn.Count > 0)
        {
            writer.WritePropertyName("x-revalidateOn");
            writer.WriteStartArray();
            foreach (var path in field.RevalidateOn) writer.WriteStringValue(path);
            writer.WriteEndArray();
        }

        // `examples` is JSON Schema's own vocabulary for "values like these", which is exactly what a
        // datalist offers — no x- prefix needed.
        if (writeExamples && field.Suggestions.Count > 0)
        {
            writer.WritePropertyName("examples");
            writer.WriteStartArray();
            foreach (var s in field.Suggestions) writer.WriteStringValue(s);
            writer.WriteEndArray();
        }

        WriteAttributes(writer, "x-attributes", field.Attributes);
        WriteAttributes(writer, "x-inputAttributes", field.InputAttributes);

        BormConditionJson.WriteIfRepresentable(writer, "x-visibleWhen", field.VisibleWhen);
        BormConditionJson.WriteIfRepresentable(writer, "x-disabledWhen", field.DisabledWhen);
        BormConditionJson.WriteIfRepresentable(writer, "x-readOnlyWhen", field.ReadOnlyWhen);
        BormConditionJson.WriteIfRepresentable(writer, "x-requiredWhen", field.RequiredWhen);

        // An expression is text, so unlike a delegate it travels. A computed value built from one is
        // recognised by the delegate's target, which is the expression itself.
        if (field.Computed?.Target is BormExpression computed) writer.WriteString("x-computed", computed.Text);
        WriteRules(writer, field.Validators, formLevel: false);
    }

    /// <summary>
    /// Writes the expression rules among <paramref name="rules"/> as <c>x-rules</c>. Rules backed by code
    /// are left out, as they always have been; a rule wrapped by <c>When(…)</c> is too, since its
    /// condition is a delegate.
    /// </summary>
    private static void WriteRules(Utf8JsonWriter writer, IEnumerable<IBormValidationRule> rules, bool formLevel)
    {
        var expressions = rules.OfType<BormExpressionRule>().ToList();
        if (expressions.Count == 0) return;

        writer.WritePropertyName("x-rules");
        writer.WriteStartArray();
        foreach (var rule in expressions)
        {
            writer.WriteStartObject();
            writer.WriteString("expression", rule.Expression.Text);
            if (rule.Message is not null) writer.WriteString("message", rule.Message);
            if (rule.Severity != BormValidationSeverity.Error)
                writer.WriteString("severity", rule.Severity.ToString().ToLowerInvariant());
            if (formLevel && rule.FieldPath is not null) writer.WriteString("field", rule.FieldPath);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a renderer-hint bag. Values that have no JSON form (a delegate, a component type someone
    /// stashed there, a number that is not finite) are skipped rather than approximated, on the same
    /// principle as the conditions.
    /// </summary>
    private static void WriteAttributes(Utf8JsonWriter writer, string property, IDictionary<string, object?> attributes)
    {
        if (attributes.Count == 0) return;

        var writable = attributes.Where(a => IsJsonWritable(a.Value)).ToList();
        if (writable.Count == 0) return;

        writer.WritePropertyName(property);
        writer.WriteStartObject();
        foreach (var (key, value) in writable)
        {
            writer.WritePropertyName(key);
            BormConditionJson.WriteValue(writer, value);
        }
        writer.WriteEndObject();
    }

    private static bool IsJsonWritable(object? value) => value switch
    {
        null or string or bool or decimal => true,
        sbyte or byte or short or ushort or int or uint or long or ulong => true,
        double d => double.IsFinite(d),
        float f => float.IsFinite(f),
        DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Guid => true,
        _ => false
    };

    private static void WriteSteps(Utf8JsonWriter writer, BormDefinition form)
    {
        if (!form.HasSteps) return;

        writer.WritePropertyName("x-steps");
        writer.WriteStartArray();
        foreach (var step in form.Steps)
        {
            writer.WriteStartObject();
            writer.WriteString("id", step.Id);
            if (step.Title is not null) writer.WriteString("title", step.Title);
            if (step.Description is not null) writer.WriteString("description", step.Description);

            writer.WritePropertyName("fields");
            writer.WriteStartArray();
            foreach (var f in step.Fields) writer.WriteStringValue(f);
            writer.WriteEndArray();

            BormConditionJson.WriteIfRepresentable(writer, "visibleWhen", step.VisibleWhen);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes an option value using the field's own JSON type, so a numeric enum comes back as numbers
    /// rather than strings on re-import. A value is only written as a number or boolean when that is
    /// exactly how it reads back — "1,000" or "007" stay text rather than quietly becoming 1000 and 7,
    /// and "NaN" stays text rather than failing the export.
    /// </summary>
    private static void WriteOptionValue(Utf8JsonWriter writer, string value, string jsonType)
    {
        switch (jsonType)
        {
            case "integer" when TryInteger(value, out var l):
                writer.WriteNumberValue(l);
                break;
            case "number" when TryNumber(value, out var d):
                writer.WriteNumberValue(d);
                break;
            case "boolean" when value is "true" or "false":
                writer.WriteBooleanValue(value == "true");
                break;
            default:
                writer.WriteStringValue(value);
                break;
        }
    }

    /// <summary>Whether <see cref="WriteOptionValue"/> writes <paramref name="value"/> as a <paramref name="jsonType"/> token.</summary>
    private static bool IsTypedToken(string value, string jsonType) => jsonType switch
    {
        "integer" => TryInteger(value, out _),
        "number" => TryNumber(value, out _),
        "boolean" => value is "true" or "false",
        _ => true
    };

    private static bool TryInteger(string value, out long result)
        => long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result)
           && result.ToString(CultureInfo.InvariantCulture) == value;

    private static bool TryNumber(string value, out double result)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
           && IsFinite(result) && BormValueConverter.ToInvariantString(result) == value;

    /// <summary>
    /// The JSON type a field's CLR value type stands for — the element type, for a collection — or
    /// null when it is text (or an enum, whose options are its member names).
    /// </summary>
    private static string? ValueJsonType(Type? valueType)
    {
        if (valueType is null) return null;
        var t = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (t != typeof(string))
        {
            if (t.IsArray) t = t.GetElementType()!;
            else if (t.IsGenericType && t.GetGenericArguments() is [var element]
                     && typeof(IEnumerable<>).MakeGenericType(element).IsAssignableFrom(t))
                t = element;
        }
        t = Nullable.GetUnderlyingType(t) ?? t;
        if (t.IsEnum) return null;

        return Type.GetTypeCode(t) switch
        {
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32
                or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 => "integer",
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "number",
            TypeCode.Boolean => "boolean",
            _ => null
        };
    }

    private static bool IsFinite(double value) => double.IsFinite(value);

    private static (string JsonType, string? Format, string? Widget) MapType(BormFieldType type) => type switch
    {
        BormFieldType.Integer => ("integer", null, null),
        BormFieldType.Number => ("number", null, null),
        BormFieldType.Range => ("number", null, "range"),
        BormFieldType.Checkbox => ("boolean", null, null),
        // A repeater says so explicitly. `"type": "array"` alone is ambiguous — a set of tick boxes is
        // an array too — and a document that does not name its control is read with the friendlier
        // default, which for a closed set of choices is a multi-select. Naming it keeps a schema this
        // library exported coming back as the form it left as.
        BormFieldType.Array => ("array", null, "array"),
        BormFieldType.Email => ("string", "email", null),
        BormFieldType.Password => ("string", "password", null),
        BormFieldType.Url => ("string", "uri", null),
        BormFieldType.Date => ("string", "date", null),
        BormFieldType.DateTime => ("string", "date-time", null),
        BormFieldType.Time => ("string", "time", null),
        // JSON Schema has no format for a month or a week, so the value travels as the picker's text
        // and the control as a widget.
        BormFieldType.Month => ("string", null, "month"),
        BormFieldType.Week => ("string", null, "week"),
        BormFieldType.Color => ("string", "color", null),
        BormFieldType.TextArea => ("string", null, "textarea"),
        // A dropdown whose choices arrive at run time (a cascading select) has no `enum` to give it
        // away, and without a widget it came back as a plain text box the app could not re-attach its
        // provider to.
        BormFieldType.Select => ("string", null, "select"),
        BormFieldType.Radio => ("string", null, "radio"),
        BormFieldType.MultiSelect => ("array", null, "multiselect"),
        // A combobox stores one of the enumerated values, exactly as a select does; only the control
        // differs, which is precisely what x-widget is for.
        BormFieldType.Combobox => ("string", null, "combobox"),
        BormFieldType.Tags => ("array", null, "tags"),
        BormFieldType.Tel => ("string", null, "tel"),
        BormFieldType.Search => ("string", null, "search"),
        BormFieldType.File => ("string", "binary", "file"),
        BormFieldType.Hidden => ("string", null, "hidden"),
        BormFieldType.Static => ("null", null, "static"),
        BormFieldType.Custom => ("string", null, "custom"),
        _ => ("string", null, null)
    };
}
