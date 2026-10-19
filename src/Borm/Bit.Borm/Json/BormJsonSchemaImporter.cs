using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.Borm;

/// <summary>
/// Builds a <see cref="BormDefinition"/> from a JSON Schema document (draft-07 through 2020-12).
/// </summary>
/// <remarks>
/// <para>
/// Supported keywords: <c>type</c> (including nullable unions such as <c>["string","null"]</c>),
/// boolean subschemas, <c>properties</c>, <c>required</c>, <c>enum</c>, <c>const</c>, <c>format</c>,
/// <c>default</c>, <c>examples</c>, <c>title</c>, <c>description</c>, <c>readOnly</c>,
/// <c>minimum</c>/<c>maximum</c> and their exclusive forms (the draft-04 boolean spelling included),
/// <c>multipleOf</c>, <c>minLength</c>/<c>maxLength</c>, <c>pattern</c>, <c>items</c>,
/// <c>minItems</c>/<c>maxItems</c>, <c>uniqueItems</c>, <c>$ref</c> into <c>$defs</c>/<c>definitions</c>
/// (with its sibling keywords, and recursive references cut off at the cycle), <c>allOf</c> (merged),
/// <c>if</c>/<c>then</c>/<c>else</c> and <c>dependentRequired</c>/<c>dependencies</c> (both mapped onto
/// conditional requiredness).
/// </para>
/// <para>
/// UI intent that JSON Schema cannot express travels in <c>x-</c> extensions: <c>x-widget</c>,
/// <c>x-order</c>, <c>x-placeholder</c>, <c>x-autocomplete</c>, <c>x-columns</c>, <c>x-colSpan</c>,
/// <c>x-accept</c>, <c>x-multiple</c>, <c>x-clearOnHide</c>, <c>x-visibleWhen</c>,
/// <c>x-disabledWhen</c>, <c>x-readOnlyWhen</c>, <c>x-requiredWhen</c>, <c>x-steps</c>,
/// <c>x-allowCustom</c>/<c>x-enum</c> and <c>enumNames</c>.
/// </para>
/// </remarks>
public static class BormJsonSchemaImporter
{
    /// <summary>Guards against a chain of <c>$ref</c>s that points back into itself.</summary>
    private const int MaxRefDepth = 32;

    /// <summary>
    /// How deeply objects and arrays may nest. A form forty levels deep is not a form anyone can fill
    /// in; a schema that describes one is a mistake or an attack, and is refused rather than expanded.
    /// </summary>
    private const int MaxNestingDepth = 32;

    /// <summary>
    /// How many fields one document may expand to. Two references to the same definition, repeated a
    /// dozen levels down, describe millions of fields in a few hundred bytes — and the importer used to
    /// build every one of them.
    /// </summary>
    private const int MaxFields = 10_000;

    /// <summary>
    /// How many merges — allOf branches folded in, references with sibling keywords — one document may
    /// need. Generous per field (a field with a four-branch allOf is ordinary), but bounded: each merge
    /// copies a schema, and a few kilobytes of allOf can ask for millions of them.
    /// </summary>
    private const int MaxMergeSteps = MaxFields * 8;

    /// <summary>
    /// How deeply <c>allOf</c> may nest — a definition extending one that extends another, and so on.
    /// Each level is a nested call, and a chain a few thousand long overflowed the stack.
    /// </summary>
    private const int MaxAllOfDepth = 64;

    /// <summary>
    /// How many times one definition may appear on a single path from the root. Twice lets a recursive
    /// shape be followed one level, and lets a definition both extend and contain another.
    /// </summary>
    private const int MaxRecursion = 2;

    /// <summary>How many rounds of union-collapsing and allOf-folding one subschema may take.</summary>
    private const int MaxPreparePasses = 8;

    /// <summary>
    /// The pattern that stands in for <c>"format": "uuid"</c> as the browser's own <c>pattern</c>
    /// attribute. The exporter recognises it so the field goes back out as the format it came in as.
    /// </summary>
    internal const string UuidPattern = "^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$";

    /// <summary>
    /// Where <see cref="ImportContext"/> keeps the <c>if</c>/<c>then</c>/<c>else</c> blocks it collects
    /// while merging <c>allOf</c>. Each branch may carry its own conditional — that is the standard way
    /// to write more than one — and a single <c>if</c> slot would keep only the last of them.
    /// </summary>
    private const string ConditionalsKeyword = "$borm:conditionals";

    /// <summary>
    /// Where a merge keeps the patterns beyond the first. Two <c>allOf</c> branches with a
    /// <c>pattern</c> each mean the value must match both, and a single <c>pattern</c> slot kept one.
    /// </summary>
    private const string PatternsKeyword = "$borm:patterns";

    /// <summary>One labelled value of an <c>anyOf</c>/<c>oneOf</c> of <c>const</c>s.</summary>
    private readonly record struct ConstChoice(JsonElement Value, string? Title);

    public static BormDefinition Import(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Import(doc.RootElement);
    }

    /// <summary>
    /// Parses a schema, returning false instead of throwing when the document is not valid JSON, is not
    /// a schema, or describes a form too large or too deeply nested to build — the shape you want
    /// behind a "paste your schema here" editor.
    /// </summary>
    public static bool TryImport(string json, out BormDefinition definition, out string? error)
    {
        try
        {
            definition = Import(json);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            definition = new BormDefinition();
            error = ex.Message;
            return false;
        }
    }

    public static BormDefinition Import(JsonElement root)
    {
        var form = new BormDefinition();

        // `true` and `false` are schemas too — "anything" and "nothing" — and neither has a field in it.
        if (root.ValueKind is JsonValueKind.True or JsonValueKind.False) return form;
        if (root.ValueKind != JsonValueKind.Object)
            throw new FormatException($"A JSON Schema document is an object (or true/false), not {root.ValueKind}.");

        var ctx = new ImportContext(root);

        // allOf is the standard "extend this schema" idiom; merging it first means the branches'
        // properties and required lists are visible to everything below.
        var merged = ctx.Prepare(root, out var identity, out _);
        if (merged.ValueKind != JsonValueKind.Object) return form;

        // The document itself is being expanded, so `"$ref": "#"` anywhere inside it is a cycle.
        identity.Add("#");
        ctx.Enter(identity, countsAsField: false);
        try
        {
            form.Title = ReadString(merged, "title");
            form.Description = ReadString(merged, "description");
            if (TryReadInt(merged, "x-columns", out var cols))
                form.Columns = Math.Max(1, cols);

            var required = ReadRequired(merged);
            if (TryGet(merged, "properties", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in props.EnumerateObject())
                    if (BuildField(ctx, prop.Name, prop.Value, required.Contains(prop.Name)) is { } field)
                        form.Fields.Add(field);
            }

            ApplyConditionalRequiredness(ctx, merged, form.Fields);
            ReadSteps(merged, form);
        }
        finally
        {
            ctx.Exit(identity);
        }

        // Order fields by x-order when present.
        form.Fields = form.Fields.OrderBy(f => f.Order).ToList();
        return form;
    }

    // ---------------------------------------------------------------- fields

    /// <summary>
    /// Builds one field, or returns null when there is no field to build: a <c>false</c> schema (no
    /// value is allowed, so there is nothing to ask for) or a reference back into a definition that is
    /// already being expanded above it as often as it may be.
    /// </summary>
    /// <remarks>
    /// A recursive schema — a category with sub-categories, a comment with replies — describes an
    /// unbounded tree, and a form is a finite set of controls. Following the reference again expanded
    /// the same definition forever, which ended in a stack overflow that took the whole process down
    /// with it: no <c>catch</c> can stop one. The recursion is followed one level, and the property
    /// that would repeat it again is left out, exactly as a condition with no JSON form is left out
    /// rather than approximated.
    /// </remarks>
    private static BormFieldDefinition? BuildField(ImportContext ctx, string name, JsonElement rawSchema, bool required)
    {
        var schema = ctx.Prepare(rawSchema, out var identity, out var recursive);
        if (recursive || schema.ValueKind == JsonValueKind.False) return null;

        // Anything else that is not an object — `true`, or a malformed value — constrains nothing.
        if (schema.ValueKind != JsonValueKind.Object) schema = ImportContext.EmptySchema;

        ctx.Enter(identity, countsAsField: true);
        try
        {
            return BuildPreparedField(ctx, name, schema, required);
        }
        finally
        {
            ctx.Exit(identity);
        }
    }

    private static BormFieldDefinition BuildPreparedField(ImportContext ctx, string name, JsonElement schema, bool required)
    {
        var type = ReadTypeName(schema);
        var format = ReadString(schema, "format");
        var widget = ReadString(schema, "x-widget");

        // Read once per field: a labelled union can have thousands of branches, and every question
        // below — is this a select, what are its options — is asked of the same answer.
        var constChoices = ctx.ConstBranches(schema);
        var fieldType = MapType(constChoices is not null, type, format, schema);
        var field = new BormFieldDefinition(name, fieldType)
        {
            Label = ReadString(schema, "title") ?? BormFieldBuilder.Humanize(name),
            HelpText = ReadString(schema, "description"),
            Placeholder = ReadString(schema, "x-placeholder"),
            Group = ReadString(schema, "x-group"),
            Autocomplete = ReadString(schema, "x-autocomplete"),
            InputMode = ReadString(schema, "x-inputMode"),
            Prefix = ReadString(schema, "x-prefix"),
            Suffix = ReadString(schema, "x-suffix"),
            Accept = ReadString(schema, "x-accept"),
            Required = required
        };

        if (TryReadInt(schema, "x-order", out var o)) field.Order = o;
        if (TryReadInt(schema, "x-colSpan", out var s)) field.ColumnSpan = s;
        if (ReadBool(schema, "readOnly") == true) field.ReadOnly = true;
        if (ReadBool(schema, "x-multiple") == true) field.Multiple = true;
        if (ReadBool(schema, "x-clearOnHide") == true) field.ClearOnHide = true;
        if (ReadBool(schema, "x-autofocus") == true) field.Autofocus = true;
        if (ReadBool(schema, "x-showLabel") == false) field.ShowLabel = false;
        if (ReadBool(schema, "x-characterCount") == true) field.ShowCharacterCount = true;
        if (ReadString(schema, "x-updateOn") == "input")
            field.UpdateOn = BormUpdateTrigger.Input;
        if (TryReadInt(schema, "x-debounce", out var ms) && ms > 0)
            field.DebounceMilliseconds = ms;
        if (TryReadLong(schema, "x-maxFileSize", out var bytes))
            field.MaxFileSize = bytes;
        if (TryReadDouble(schema, "x-step", out var stepValue))
            field.NumericStep = stepValue;
        if (TryGet(schema, "x-revalidateOn", out var revalidate) && revalidate.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in revalidate.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String) continue;
                if (item.GetString() is { Length: > 0 } dependency && !field.RevalidateOn.Contains(dependency))
                    field.RevalidateOn.Add(dependency);
            }
        }

        // `examples` is JSON Schema's own "values like these", which is what a datalist offers.
        ReadExamples(schema, field.Suggestions);

        ReadAttributes(schema, "x-attributes", field.Attributes);
        ReadAttributes(schema, "x-inputAttributes", field.InputAttributes);

        if (required)
            field.AddValidator(new BormRequiredRule());

        ApplyWidget(field, schema, widget);
        ApplyConstraints(field, schema);
        var valueKind = ApplyEnum(constChoices, field, schema, type);
        ApplyConditions(field, schema);

        if (TryGet(schema, "default", out var def))
            field.DefaultValue = BormConditionJson.ReadValue(def);

        // Composition
        if (fieldType == BormFieldType.Object
            && TryGet(schema, "properties", out var childProps) && childProps.ValueKind == JsonValueKind.Object)
        {
            var childRequired = ReadRequired(schema);
            foreach (var p in childProps.EnumerateObject())
                if (BuildField(ctx, p.Name, p.Value, childRequired.Contains(p.Name)) is { } child)
                    field.Children.Add(child);

            ApplyConditionalRequiredness(ctx, schema, field.Children);
            field.Children = field.Children.OrderBy(c => c.Order).ToList();
        }
        // The declared JSON type decides this, not the control the field ended up as. A multi-select is
        // `"type": "array"` carrying its choices as a top-level `enum`, which reads as a Select before
        // the widget hint corrects it — and testing the mapped type meant `minItems`, `maxItems` and
        // `uniqueItems` were skipped for exactly the fields most likely to declare them. "Choose at
        // least one" survived the export and was dropped on the way back in.
        else if (IsArraySchema(type, fieldType, field.Type))
        {
            valueKind = BuildArrayItems(ctx, field, schema, widget) ?? valueKind;
        }

        ApplyChoiceClosure(field, schema);
        ApplyValueType(field, valueKind);
        return field;
    }

    /// <summary>
    /// Whether this schema describes an array's contents, whatever control it renders as. A field is
    /// one when the document says <c>"type": "array"</c>, or when the mapping arrived at a type that
    /// holds several values.
    /// </summary>
    private static bool IsArraySchema(string? declaredType, BormFieldType mapped, BormFieldType actual)
        => string.Equals(declaredType, "array", StringComparison.Ordinal)
           || mapped == BormFieldType.Array
           || actual is BormFieldType.Array or BormFieldType.MultiSelect or BormFieldType.Tags;

    /// <summary>Reads what an array holds. Returns the JSON type of its choices, when they have one.</summary>
    private static string? BuildArrayItems(
        ImportContext ctx, BormFieldDefinition field, JsonElement schema, string? widget)
    {
        string? valueKind = null;
        var hasItems = TryGet(schema, "items", out var items);

        // A field the widget hint has already settled — an exported multi-select or tag list — has no
        // per-row template to build; only its size constraints and per-item rules are still to read.
        if (field.Type is BormFieldType.Array)
        {
            // 2020-12 renamed the tuple form to prefixItems; the single-schema `items` is what forms use.
            if (hasItems && items.ValueKind != JsonValueKind.Array)
                field.ItemTemplate = BuildField(ctx, "item", items, required: false);

            // A repeater cannot render a row it has no shape for, and an array with no `items` (or a
            // tuple, or a row type that only refers back to itself) is still an array the user can
            // fill in. A text box per row is the honest reading of "a list of something": it is what
            // the browser would take for any of them, and the form renders instead of throwing on the
            // first "Add".
            field.ItemTemplate ??= new BormFieldDefinition("item", BormFieldType.Text)
            {
                Label = BormFieldBuilder.Humanize("item")
            };
        }

        var uniqueItems = ReadBool(schema, "uniqueItems") == true;

        // An array of a fixed set of choices, each usable once, is a set of tick boxes rather than a
        // repeater — "add a row, open a dropdown, pick Monday; add a row, open a dropdown, pick
        // Tuesday" is the wrong shape for a question with three answers. This is the rule
        // react-jsonschema-form uses, and `uniqueItems` is what makes it safe: without it the document
        // is explicitly allowing the same value twice, which only a repeater can express.
        //
        // A document that named its own control is taken at its word, so a schema this library
        // exported — which always writes `x-widget` — comes back as the form it left as. The default
        // is only for a schema that never said.
        if (uniqueItems && widget is null
            && field.Type == BormFieldType.Array
            && field.ItemTemplate is { Type: BormFieldType.Select, Options.Count: > 0 } choices)
        {
            field.Type = BormFieldType.MultiSelect;
            foreach (var option in choices.Options) field.Options.Add(option);
            valueKind = JsonKindOf(choices.ValueType);
            field.ItemTemplate = null;
        }

        // The standard spelling of a multi-select keeps its choices on `items`, where JSON Schema says
        // they belong — a top-level `enum` describes the array itself, so no list of answers could ever
        // satisfy it. A document that named the control still has to have its choices read from there.
        if (field.Type == BormFieldType.MultiSelect && field.Options.Count == 0 && hasItems)
        {
            var itemSchema = ctx.Prepare(items, out _, out _);
            if (itemSchema.ValueKind == JsonValueKind.Object)
                valueKind = ReadChoices(ctx.ConstBranches(itemSchema), itemSchema, field.Options, ReadTypeName(itemSchema));
        }

        // A tag list's length limits, pattern and suggestions are about each tag, so a standard
        // document puts them on `items`. Read from there unless the field already says otherwise.
        if (field.Type == BormFieldType.Tags && hasItems)
        {
            var itemSchema = ctx.Prepare(items, out _, out _);
            if (itemSchema.ValueKind == JsonValueKind.Object)
            {
                if (field.MinLength is null && TryReadInt(itemSchema, "minLength", out var minLen))
                {
                    field.MinLength = minLen;
                    field.AddValidator(new BormMinLengthRule(minLen));
                }
                if (field.MaxLength is null && TryReadInt(itemSchema, "maxLength", out var maxLen))
                {
                    field.MaxLength = maxLen;
                    field.AddValidator(new BormMaxLengthRule(maxLen));
                }
                if (field.Pattern is null) ApplyPatterns(field, itemSchema);
                ReadExamples(itemSchema, field.Suggestions);
            }
        }

        if (TryReadInt(schema, "minItems", out var minI)) field.MinItems = minI;
        if (TryReadInt(schema, "maxItems", out var maxI)) field.MaxItems = maxI;
        if (field.MinItems is not null || field.MaxItems is not null)
            field.AddValidator(new BormCollectionSizeRule(field.MinItems, field.MaxItems));

        // A set of tick boxes cannot hold the same answer twice, so the rule would have nothing to say.
        if (uniqueItems && field.Type != BormFieldType.MultiSelect)
            field.AddValidator(new BormUniqueItemsRule());

        return valueKind;
    }

    // ---------------------------------------------------------------- keyword mapping

    /// <summary>
    /// Reads <c>type</c>, tolerating the union form used for nullable values —
    /// <c>["string","null"]</c> means "a string, optionally absent".
    /// </summary>
    private static string? ReadTypeName(JsonElement schema)
    {
        if (!TryGet(schema, "type", out var type)) return null;
        if (type.ValueKind == JsonValueKind.String) return type.GetString();
        if (type.ValueKind != JsonValueKind.Array) return null;

        return type.EnumerateArray()
            .Where(t => t.ValueKind == JsonValueKind.String)
            .Select(t => t.GetString())
            .FirstOrDefault(t => t is not null && !string.Equals(t, "null", StringComparison.Ordinal));
    }

    private static BormFieldType MapType(bool constUnion, string? type, string? format, JsonElement schema)
    {
        if (TryGet(schema, "enum", out _) || TryGet(schema, "x-enum", out _)) return BormFieldType.Select;
        if (constUnion) return BormFieldType.Select;

        return type switch
        {
            "integer" => BormFieldType.Integer,
            "number" => BormFieldType.Number,
            "boolean" => BormFieldType.Checkbox,
            "object" => BormFieldType.Object,
            "array" => BormFieldType.Array,
            "string" => MapStringFormat(format, schema),
            // No type at all: an object with properties is still an object.
            null when TryGet(schema, "properties", out _) => BormFieldType.Object,
            null when TryGet(schema, "items", out _) => BormFieldType.Array,
            _ => BormFieldType.Text
        };
    }

    private static BormFieldType MapStringFormat(string? format, JsonElement schema)
    {
        // A base64/binary payload is a file upload, not a text box.
        if (ReadString(schema, "contentEncoding") is "base64" || ReadString(schema, "contentMediaType") is not null)
            return BormFieldType.File;

        return format switch
        {
            "email" or "idn-email" => BormFieldType.Email,
            "password" => BormFieldType.Password,
            "uri" or "url" or "iri" => BormFieldType.Url,
            "date" => BormFieldType.Date,
            "date-time" => BormFieldType.DateTime,
            "time" => BormFieldType.Time,
            "color" => BormFieldType.Color,
            "tel" or "phone" => BormFieldType.Tel,
            "binary" => BormFieldType.File,
            _ => BormFieldType.Text
        };
    }

    private static void ApplyWidget(BormFieldDefinition field, JsonElement schema, string? widget)
    {
        // An explicit renderer key wins over everything: it names a component the app registered, and
        // no built-in type can substitute for it.
        if (ReadString(schema, "x-renderer") is { Length: > 0 } renderer)
        {
            field.CustomRenderer = renderer;
            if (widget is null or "custom") field.Type = BormFieldType.Custom;
        }

        if (string.IsNullOrEmpty(widget)) return;

        field.Type = widget switch
        {
            "array" => BormFieldType.Array,
            "textarea" => BormFieldType.TextArea,
            "radio" => BormFieldType.Radio,
            "multiselect" => BormFieldType.MultiSelect,
            "combobox" => BormFieldType.Combobox,
            "tags" => BormFieldType.Tags,
            "range" => BormFieldType.Range,
            "color" => BormFieldType.Color,
            "tel" => BormFieldType.Tel,
            "search" => BormFieldType.Search,
            "password" => BormFieldType.Password,
            "select" => BormFieldType.Select,
            "file" => BormFieldType.File,
            "hidden" => BormFieldType.Hidden,
            "static" => BormFieldType.Static,
            "switch" => BormFieldType.Checkbox,
            _ => field.Type
        };

        // "switch" is a checkbox with a different affordance, so the intent travels as a renderer hint
        // rather than as a field type of its own.
        if (widget == "switch") field.Attributes["switch"] = true;
    }

    /// <summary>Reads a renderer-hint bag written by the exporter back onto a field.</summary>
    private static void ReadAttributes(JsonElement schema, string property, IDictionary<string, object?> into)
    {
        if (!TryGet(schema, property, out var bag) || bag.ValueKind != JsonValueKind.Object) return;
        foreach (var entry in bag.EnumerateObject())
            into[entry.Name] = BormConditionJson.ReadValue(entry.Value);
    }

    /// <summary>
    /// Reads <c>examples</c> as datalist suggestions. An example is a whole value, so for an array
    /// field each one is itself a list — its members are what a tag box should propose, not the text
    /// of the list object.
    /// </summary>
    private static void ReadExamples(JsonElement schema, IList<string> into)
    {
        if (!TryGet(schema, "examples", out var examples) || examples.ValueKind != JsonValueKind.Array) return;

        foreach (var example in examples.EnumerateArray())
        {
            if (example.ValueKind == JsonValueKind.Array)
            {
                foreach (var member in example.EnumerateArray()) Add(member);
            }
            else
            {
                Add(example);
            }
        }

        void Add(JsonElement value)
        {
            if (value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) return;
            if (OptionValue(value) is { } text && !into.Contains(text)) into.Add(text);
        }
    }

    private static void ApplyConstraints(BormFieldDefinition field, JsonElement schema)
    {
        if (TryReadInt(schema, "minLength", out var minLen))
        {
            field.MinLength = minLen;
            field.AddValidator(new BormMinLengthRule(minLen));
        }
        if (TryReadInt(schema, "maxLength", out var maxLen))
        {
            field.MaxLength = maxLen;
            field.AddValidator(new BormMaxLengthRule(maxLen));
        }

        double? min = null, max = null;
        bool minExclusive = false, maxExclusive = false;
        if (TryReadDouble(schema, "minimum", out var mnv)) min = mnv;
        if (TryReadDouble(schema, "maximum", out var mxv)) max = mxv;

        // Both bounds apply when both are given, so the stricter one is the one to keep: "at least 10
        // and greater than 0" is "at least 10", not "greater than 0". An exclusive bound equal to the
        // inclusive one is the stricter of the two.
        if (TryReadDouble(schema, "exclusiveMinimum", out var emnv))
        {
            if (min is null || emnv >= min) (min, minExclusive) = (emnv, true);
        }
        // Draft-04 — and OpenAPI 3.0, which still speaks it — spells exclusivity as a boolean that
        // qualifies `minimum`. Reading the number out of a boolean is what used to fail the import.
        else if (ReadBool(schema, "exclusiveMinimum") == true && min is not null)
        {
            minExclusive = true;
        }

        if (TryReadDouble(schema, "exclusiveMaximum", out var emxv))
        {
            if (max is null || emxv <= max) (max, maxExclusive) = (emxv, true);
        }
        else if (ReadBool(schema, "exclusiveMaximum") == true && max is not null)
        {
            maxExclusive = true;
        }

        if (min is not null || max is not null)
        {
            // The field keeps the bound itself — it only becomes the input's min/max hint — while the
            // rule carries whether the bound is allowed. Nudging the number instead was wrong for
            // integers with a fractional bound, leaked "99.9999999" into the message the user reads,
            // and exported as an inclusive bound, so the exclusive meaning was lost for good.
            field.Min = min;
            field.Max = max;
            field.AddValidator(new BormRangeRule(min, max)
            {
                MinExclusive = minExclusive,
                MaxExclusive = maxExclusive
            });
        }

        if (TryReadDouble(schema, "multipleOf", out var factor) && factor > 0)
        {
            field.NumericStep ??= factor;
            field.AddValidator(new BormMultipleOfRule(factor));
        }

        ApplyPatterns(field, schema);

        // A `format` that maps to a semantic rule gets that rule too, not just the input type.
        switch (ReadString(schema, "format"))
        {
            case "email" or "idn-email":
                field.AddValidator(new BormEmailRule());
                break;
            case "uri" or "url" or "iri":
                field.AddValidator(new BormUrlRule());
                break;
            case "uuid":
                // A rule of its own rather than a pattern rule: a document that also declares a
                // `pattern` means both, and sharing the "pattern" key made one replace the other.
                field.Pattern ??= UuidPattern;
                field.AddValidator(new BormUuidRule());
                break;
        }
    }

    /// <summary>
    /// Reads <c>const</c>, a list of labelled <c>const</c> branches, or <c>enum</c> onto the field.
    /// Returns the JSON type the values share — <c>integer</c>, <c>number</c> or <c>boolean</c> — so the
    /// chosen answer can be stored as that type rather than as its text.
    /// </summary>
    private static string? ApplyEnum(
        IReadOnlyList<ConstChoice>? constChoices, BormFieldDefinition field, JsonElement schema, string? declaredType)
    {
        // `const` is a one-value enum: the field is fixed, so it is shown read-only with that value.
        if (TryGet(schema, "const", out var constant))
        {
            field.DefaultValue = BormConditionJson.ReadValue(constant);
            field.ReadOnly = true;
            return null;
        }

        if (!field.IsChoice
            && constChoices is null
            && !TryGet(schema, "enum", out _) && !TryGet(schema, "x-enum", out _))
            return null;

        var kind = ReadChoices(constChoices, schema, field.Options, declaredType);
        if (field.Options.Count > 0 && !field.IsChoice) field.Type = BormFieldType.Select;
        return kind;
    }

    /// <summary>
    /// Reads the choices a schema offers into <paramref name="into"/>: either a labelled list of
    /// <c>const</c> branches, or <c>enum</c> (or <c>x-enum</c>, for a combobox that also accepts other
    /// answers) with its <c>enumNames</c>, <c>x-enumGroups</c> and <c>x-enumDisabled</c>.
    /// </summary>
    /// <remarks>
    /// A choice with no values listed — a select fed by a provider — still has the type its schema
    /// declares, so it is returned all the same: otherwise an integer select exported, imported and
    /// exported again came back as a string one.
    /// </remarks>
    private static string? ReadChoices(
        IReadOnlyList<ConstChoice>? constChoices, JsonElement schema, IList<BormSelectOption> into, string? declaredType)
    {
        // `oneOf` of `const` values is the standard way to give an enum member a display label, since
        // JSON Schema's own `enum` has nowhere to put one.
        if (constChoices is not null)
        {
            foreach (var choice in constChoices)
            {
                // A null member is the "no answer" every optional select already offers.
                if (OptionValue(choice.Value) is not { } value) continue;
                into.Add(new BormSelectOption(value, choice.Title ?? value));
            }
            return ValueKind(constChoices.Select(c => c.Value), declaredType);
        }

        var values = TryGet(schema, "enum", out var en) && en.ValueKind == JsonValueKind.Array ? en
            : TryGet(schema, "x-enum", out var xen) && xen.ValueKind == JsonValueKind.Array ? xen
            : default;
        if (values.ValueKind != JsonValueKind.Array)
            return declaredType is "integer" or "number" or "boolean" ? declaredType : null;

        var names = ReadStringArray(schema, "enumNames");
        var groups = ReadStringArray(schema, "x-enumGroups");

        // A set rather than a positional array: which options are disabled is a property of the values,
        // not of their order, and a schema hand-written by someone else will spell it that way.
        var disabled = new HashSet<string>(StringComparer.Ordinal);
        if (TryGet(schema, "x-enumDisabled", out var dis) && dis.ValueKind == JsonValueKind.Array)
            foreach (var d in dis.EnumerateArray())
                if (OptionValue(d) is { } value) disabled.Add(value);

        // Labels and groups are positional, so a skipped null member still takes up its slot.
        var i = 0;
        foreach (var item in values.EnumerateArray())
        {
            if (OptionValue(item) is { } value)
            {
                var label = names is not null && i < names.Length ? names[i] : value;
                var group = groups is not null && i < groups.Length && groups[i].Length > 0 ? groups[i] : null;
                into.Add(new BormSelectOption(value, label, disabled.Contains(value), group));
            }
            i++;
        }

        return ValueKind(values.EnumerateArray(), declaredType);
    }

    /// <summary>
    /// The text an option value is stored and compared as. It has to be exactly what the control
    /// renders the bound value as (<see cref="BormValueConverter.ToInputString"/>), or the
    /// current answer is never shown as selected: <c>true</c> is <c>"true"</c> rather than .NET's
    /// <c>"True"</c>, and <c>1.0</c> is <c>"1"</c>. Null is not a choice — it is the absence of one.
    /// </summary>
    private static string? OptionValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.TryGetInt64(out var l)
            ? l.ToString(CultureInfo.InvariantCulture)
            : value.TryGetDouble(out var d) ? BormValueConverter.ToInvariantString(d) : value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => value.GetRawText()
    };

    /// <summary>
    /// The JSON type a set of choice values shares: the declared one when the schema says and every
    /// value really is of it, otherwise whatever every value turns out to be. Null when they are
    /// strings, or a mixture.
    /// </summary>
    /// <remarks>
    /// A declaration the values contradict — <c>"type": "number"</c> over <c>["0.50", "1.50"]</c> — is
    /// not believed. Typing that field as a number stored 1.5 for the choice "1.50", which renders as
    /// "1.5" and so matched none of the options it was chosen from.
    /// </remarks>
    private static string? ValueKind(IEnumerable<JsonElement> values, string? declaredType)
    {
        var present = values.Where(v => v.ValueKind != JsonValueKind.Null).ToList();

        if (declaredType is "integer" or "number" or "boolean")
            return present.All(v => IsOfKind(v, declaredType)) ? declaredType : null;
        if (declaredType is not null || present.Count == 0) return null;

        if (present.All(v => IsOfKind(v, "integer"))) return "integer";
        if (present.All(v => IsOfKind(v, "number"))) return "number";
        if (present.All(v => IsOfKind(v, "boolean"))) return "boolean";
        return null;

        static bool IsOfKind(JsonElement value, string kind) => kind switch
        {
            // 1.0 is an integer as far as JSON Schema is concerned; 1e20 is too big to store as one.
            "integer" => value.ValueKind == JsonValueKind.Number
                         && (value.TryGetInt64(out _)
                             || (value.TryGetDouble(out var d) && double.IsFinite(d) && Math.Floor(d) == d
                                 && Math.Abs(d) < long.MaxValue)),
            "number" => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var n) && double.IsFinite(n),
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            _ => false
        };
    }

    /// <summary>
    /// Reads <c>pattern</c> — and, for a schema merged from an allOf, every further pattern a branch
    /// added — onto the field. The first is the field's own (and the browser's hint); the rest are
    /// enforced beside it, since a value has to match all of them.
    /// </summary>
    private static void ApplyPatterns(BormFieldDefinition field, JsonElement schema)
    {
        if (ReadString(schema, "pattern") is not { } pattern) return;

        field.Pattern = pattern;
        field.AddValidator(new BormPatternRule(pattern));

        if (!TryGet(schema, PatternsKeyword, out var more) || more.ValueKind != JsonValueKind.Array) return;
        foreach (var extra in more.EnumerateArray())
            if (extra.ValueKind == JsonValueKind.String && extra.GetString() is { } p && p != pattern)
                field.Validators.Add(new BormAdditionalPatternRule(p));
    }

    /// <summary>The JSON type a <see cref="BormFieldDefinition.ValueType"/> stands for, if any.</summary>
    private static string? JsonKindOf(Type? valueType)
    {
        if (valueType is null) return null;
        var t = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) t = t.GetGenericArguments()[0];
        if (t == typeof(long)) return "integer";
        if (t == typeof(double)) return "number";
        if (t == typeof(bool)) return "boolean";
        return null;
    }

    /// <summary>
    /// Gives a choice field whose values are numbers or booleans a <see cref="BormFieldDefinition.ValueType"/>,
    /// so choosing "2" stores the number 2 — the type the schema declared — rather than the text the
    /// control produced. An integer enum used to import as a string select and export as a string enum.
    /// </summary>
    private static void ApplyValueType(BormFieldDefinition field, string? kind)
    {
        if (kind is null || field.ValueType is not null || !field.IsChoice) return;

        var scalar = kind switch
        {
            "integer" => typeof(long),
            "number" => typeof(double),
            "boolean" => typeof(bool),
            _ => null
        };
        if (scalar is null) return;

        field.ValueType = field.Type == BormFieldType.MultiSelect
            ? typeof(List<>).MakeGenericType(scalar)
            : typeof(Nullable<>).MakeGenericType(scalar);
    }

    /// <summary>
    /// A combobox lets the user type, so whether an answer that is on no list is acceptable has to be
    /// said. <c>enum</c> says it is not — that is what the keyword means — and the exporter writes an
    /// open one as <c>x-allowCustom</c> with its choices under <c>x-enum</c>. Without this, a closed
    /// combobox came back from its own export accepting anything at all.
    /// </summary>
    private static void ApplyChoiceClosure(BormFieldDefinition field, JsonElement schema)
    {
        if (field.Type != BormFieldType.Combobox) return;
        if (ReadBool(schema, "x-allowCustom") == true) field.RemoveValidators("oneOf");
        else field.AddValidator(new BormOneOfRule());
    }

    private static void ApplyConditions(BormFieldDefinition field, JsonElement schema)
    {
        if (TryGet(schema, "x-visibleWhen", out var visible))
            field.VisibleWhen = BormConditionJson.Read(visible);
        if (TryGet(schema, "x-disabledWhen", out var disabled))
            field.DisabledWhen = BormConditionJson.Read(disabled);
        if (TryGet(schema, "x-readOnlyWhen", out var readOnly))
            field.ReadOnlyWhen = BormConditionJson.Read(readOnly);
        if (TryGet(schema, "x-requiredWhen", out var requiredWhen))
            field.RequiredWhen = BormConditionJson.Read(requiredWhen);
    }

    // ---------------------------------------------------------------- conditional requiredness

    /// <summary>
    /// Maps the two standard ways a schema says "this field is required only sometimes" onto
    /// <see cref="BormFieldDefinition.RequiredWhen"/>:
    /// <c>dependentRequired</c>/<c>dependencies</c> ("required once that one is filled in") and the
    /// <c>if</c>/<c>then</c> pair whose <c>if</c> pins properties to a <c>const</c> or <c>enum</c>.
    /// </summary>
    private static void ApplyConditionalRequiredness(
        ImportContext ctx, JsonElement schema, IList<BormFieldDefinition> fields)
    {
        if (TryGet(schema, "dependentRequired", out var dependent) && dependent.ValueKind == JsonValueKind.Object)
            ApplyDependentRequired(dependent, fields);

        // Draft-07 spelled it `dependencies`, with an array value meaning the same thing.
        if (TryGet(schema, "dependencies", out var dependencies) && dependencies.ValueKind == JsonValueKind.Object)
            ApplyDependentRequired(dependencies, fields);

        ApplyIfThen(ctx, schema, fields);

        // Every conditional an allOf branch carried, each applied on its own terms.
        if (TryGet(schema, ConditionalsKeyword, out var conditionals) && conditionals.ValueKind == JsonValueKind.Array)
            foreach (var block in conditionals.EnumerateArray())
                ApplyIfThen(ctx, block, fields);
    }

    private static void ApplyDependentRequired(JsonElement map, IList<BormFieldDefinition> fields)
    {
        foreach (var entry in map.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Array) continue;

            var trigger = new BormFieldCondition(entry.Name, BormConditionOperator.IsNotEmpty);
            foreach (var dependentName in entry.Value.EnumerateArray())
            {
                if (dependentName.ValueKind != JsonValueKind.String || dependentName.GetString() is not { } target) continue;
                var field = fields.FirstOrDefault(f => string.Equals(f.Name, target, StringComparison.OrdinalIgnoreCase));
                if (field is null || field.Required) continue;
                field.RequiredWhen = Combine(field.RequiredWhen, trigger);
            }
        }
    }

    private static void ApplyIfThen(ImportContext ctx, JsonElement schema, IList<BormFieldDefinition> fields)
    {
        if (!TryGet(schema, "if", out var ifSchema)) return;

        var condition = ReadIfCondition(ctx.Resolve(ifSchema));
        if (condition is null) return;

        if (TryGet(schema, "then", out var thenSchema))
            ApplyBranch(ctx.Resolve(thenSchema), condition);

        if (TryGet(schema, "else", out var elseSchema))
        {
            // "Otherwise" is the negation, which only round-trips cleanly for a single comparison.
            var negated = Negate(condition);
            if (negated is not null) ApplyBranch(ctx.Resolve(elseSchema), negated);
        }

        void ApplyBranch(JsonElement branch, IBormCondition when)
        {
            foreach (var name in ReadRequired(branch))
            {
                var field = fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
                if (field is null || field.Required) continue;
                field.RequiredWhen = Combine(field.RequiredWhen, when);
            }
        }
    }

    /// <summary>
    /// Turns an <c>if</c> subschema into a condition, for the shape forms actually use:
    /// <c>{"properties": {"kind": {"const": "business"}}}</c>, optionally with a <c>required</c> list
    /// ("once this one is filled in"). Anything more expressive is beyond what a UI condition can
    /// represent, and is ignored rather than half-applied.
    /// </summary>
    /// <remarks>
    /// "Ignored" means the whole <c>if</c>. Dropping only the parts that have no condition form — an
    /// <c>"age": {"maximum": 17}</c> next to a <c>const</c> — left a condition that fires for everyone
    /// the <c>const</c> matches, so a field meant for minors became required of every adult as well,
    /// and its <c>else</c> was wrong the other way round.
    /// </remarks>
    private static IBormCondition? ReadIfCondition(JsonElement ifSchema)
    {
        if (ifSchema.ValueKind != JsonValueKind.Object) return null;

        foreach (var keyword in ifSchema.EnumerateObject())
            if (keyword.Name is not ("properties" or "required" or "type" or "title" or "description" or "$comment"))
                return null;

        var parts = new List<IBormCondition>();
        var pinned = new HashSet<string>(StringComparer.Ordinal);

        if (TryGet(ifSchema, "properties", out var props))
        {
            if (props.ValueKind != JsonValueKind.Object) return null;
            foreach (var prop in props.EnumerateObject())
            {
                if (TryGet(prop.Value, "const", out var constant))
                {
                    parts.Add(new BormFieldCondition(prop.Name, BormConditionOperator.Equals,
                        BormConditionJson.ReadValue(constant)));
                }
                else if (TryGet(prop.Value, "enum", out var en) && en.ValueKind == JsonValueKind.Array)
                {
                    parts.Add(new BormFieldCondition(prop.Name, BormConditionOperator.In,
                        BormConditionJson.ReadValue(en)));
                }
                else
                {
                    return null;
                }
                pinned.Add(prop.Name);
            }
        }

        // `required` inside an `if` means "when this one is present" — a pinned property already is.
        foreach (var name in ReadRequired(ifSchema))
            if (!pinned.Contains(name))
                parts.Add(new BormFieldCondition(name, BormConditionOperator.IsNotEmpty));

        return parts.Count switch
        {
            0 => null,
            1 => parts[0],
            _ => new BormConditionGroup(BormConditionLogic.And, [.. parts])
        };
    }

    private static IBormCondition? Negate(IBormCondition condition) => condition switch
    {
        BormFieldCondition { Operator: BormConditionOperator.Equals } c
            => new BormFieldCondition(c.FieldPath, BormConditionOperator.NotEquals, c.Value),
        BormFieldCondition { Operator: BormConditionOperator.In } c
            => new BormFieldCondition(c.FieldPath, BormConditionOperator.NotIn, c.Value),
        BormFieldCondition { Operator: BormConditionOperator.IsNotEmpty } c
            => new BormFieldCondition(c.FieldPath, BormConditionOperator.IsEmpty),
        _ => null
    };

    private static IBormCondition Combine(IBormCondition? existing, IBormCondition addition)
        => existing is null ? addition : BormConditionGroup.Any(existing, addition);

    // ---------------------------------------------------------------- steps

    private static void ReadSteps(JsonElement schema, BormDefinition form)
    {
        if (!TryGet(schema, "x-steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            return;

        foreach (var element in steps.EnumerateArray())
        {
            var id = ReadString(element, "id");
            if (id is null) continue;

            var step = new BormStep(id, ReadString(element, "title"))
            {
                Description = ReadString(element, "description")
            };

            if (TryGet(element, "fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
                foreach (var f in fields.EnumerateArray())
                    if (f.ValueKind == JsonValueKind.String && f.GetString() is { } name) step.Fields.Add(name);

            if (TryGet(element, "visibleWhen", out var visible))
                step.VisibleWhen = BormConditionJson.Read(visible);

            form.Steps.Add(step);
        }
    }

    // ---------------------------------------------------------------- helpers
    //
    // Every read goes through these. JsonElement throws when asked for a property of something that
    // is not an object, or for a number out of something that is not one — so `"x-order": "1"`, a
    // boolean subschema, or the draft-04 `"exclusiveMinimum": true` used to fail the whole import.

    private static bool TryGet(JsonElement element, string property, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object) return element.TryGetProperty(property, out value);
        value = default;
        return false;
    }

    private static bool TryReadInt(JsonElement element, string property, out int value)
    {
        value = 0;
        return TryGet(element, property, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out value);
    }

    private static bool TryReadLong(JsonElement element, string property, out long value)
    {
        value = 0;
        return TryGet(element, property, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out value);
    }

    private static bool TryReadDouble(JsonElement element, string property, out double value)
    {
        value = 0;
        return TryGet(element, property, out var v) && v.ValueKind == JsonValueKind.Number
               && v.TryGetDouble(out value) && double.IsFinite(value);
    }

    private static bool? ReadBool(JsonElement element, string property)
        => TryGet(element, property, out var v)
            ? v.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => null }
            : null;

    private static HashSet<string> ReadRequired(JsonElement schema)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (TryGet(schema, "required", out var req) && req.ValueKind == JsonValueKind.Array)
            foreach (var r in req.EnumerateArray())
                if (r.ValueKind == JsonValueKind.String && r.GetString() is { } s) set.Add(s);
        return set;
    }

    private static string? ReadString(JsonElement element, string property)
        => TryGet(element, property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string[]? ReadStringArray(JsonElement element, string property)
        => TryGet(element, property, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : OptionValue(e) ?? "").ToArray()
            : null;

    /// <summary>Whether a union branch only says "or nothing": <c>{"type": "null"}</c>.</summary>
    private static bool DeclaresOnlyNull(JsonElement branch)
    {
        if (branch.ValueKind != JsonValueKind.Object || !TryGet(branch, "type", out var type)) return false;
        if (TryGet(branch, "const", out _) || TryGet(branch, "properties", out _)) return false;
        return type.ValueKind switch
        {
            JsonValueKind.String => type.GetString() == "null",
            JsonValueKind.Array => type.GetArrayLength() > 0
                                   && type.EnumerateArray().All(t => t.ValueKind == JsonValueKind.String && t.GetString() == "null"),
            _ => false
        };
    }

    /// <summary>
    /// Resolves <c>$ref</c> pointers and flattens <c>allOf</c> against the document they came from, and
    /// keeps the budgets that stop a hostile or recursive document from expanding without end.
    /// </summary>
    private sealed class ImportContext(JsonElement root)
    {
        /// <summary>
        /// Documents the importer writes for itself (merged schemas) are read back with room to spare:
        /// wrapping a property in an <c>allOf</c> adds two levels, and a document that only just fitted
        /// the default limit of 64 should not be refused for what the importer did to it.
        /// </summary>
        private static readonly JsonDocumentOptions DetachedOptions = new() { MaxDepth = 256 };

        /// <summary>The <c>{}</c> schema — "anything" — standing in for <c>true</c>.</summary>
        public static readonly JsonElement EmptySchema = ParseDetached("{}"u8.ToArray());

        private static JsonElement ParseDetached(byte[] utf8)
        {
            using var doc = JsonDocument.Parse(utf8, DetachedOptions);
            return doc.RootElement.Clone();
        }

        private readonly JsonElement _root = root;

        /// <summary>
        /// How many times each definition appears on the path from the document root down to the field
        /// being built. One more than <see cref="MaxRecursion"/> is a cycle.
        /// </summary>
        private readonly Dictionary<string, int> _expanding = new(StringComparer.Ordinal);

        /// <summary>Definitions whose <c>allOf</c> is being folded in right now.</summary>
        private readonly HashSet<string> _merging = new(StringComparer.Ordinal);

        /// <summary>The identity of the subschema <see cref="Prepare"/> is working on, which allOf branches add to.</summary>
        private HashSet<string>? _identity;

        private int _fields;
        private int _depth;
        private int _mergeSteps;
        private int _allOfDepth;

        /// <summary>Set when an <c>allOf</c> branch had to be dropped because it names a definition seen too often.</summary>
        private bool _recursionHit;

        /// <summary>
        /// Turns a raw subschema into the one schema object a field is built from: references followed,
        /// a null union collapsed and <c>allOf</c> merged.
        /// </summary>
        /// <param name="schema">The subschema as written.</param>
        /// <param name="identity">
        /// The definitions this field <em>is</em> — the ones its own <c>$ref</c> chain, a collapsed null
        /// union's branch and its <c>allOf</c> branches led to. They are what makes a descendant's
        /// reference to the same place a cycle.
        /// </param>
        /// <param name="recursive">True when this subschema refers back into a definition already expanded too often.</param>
        public JsonElement Prepare(JsonElement schema, out HashSet<string> identity, out bool recursive)
        {
            identity = new HashSet<string>(StringComparer.Ordinal);
            var (previousHit, previousIdentity) = (_recursionHit, _identity);
            _recursionHit = false;
            _identity = identity;
            try
            {
                var current = Resolve(schema, identity);

                // Collapsing a union can surface an allOf from the branch, and folding an allOf can
                // surface a union from one of its branches, so the two take turns until neither has
                // anything left to do. Each pass removes a level, so a handful is always enough.
                for (var pass = 0; pass < MaxPreparePasses; pass++)
                {
                    current = CollapseUnion(current, identity);
                    if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty("allOf", out _)) break;
                    current = MergeAllOf(current);
                }

                recursive = _recursionHit || identity.Any(IsExhausted);
                return current;
            }
            finally
            {
                (_recursionHit, _identity) = (previousHit, previousIdentity);
            }
        }

        /// <summary>
        /// Whether a definition already appears on the current path as often as it may. Counting rather
        /// than refusing the first repeat is what keeps a diamond working: an employee <em>is</em> a
        /// person (through allOf) and also <em>has</em> a manager who is one, and nothing about that
        /// repeats forever. A genuinely recursive definition is followed one level and cut at the next.
        /// </summary>
        private bool IsExhausted(string pointer)
            => _expanding.TryGetValue(pointer, out var count) && count >= MaxRecursion;

        /// <summary>Marks a field's definitions as being expanded, and charges it against the budgets.</summary>
        public void Enter(HashSet<string> identity, bool countsAsField)
        {
            if (countsAsField && ++_fields > MaxFields)
                throw new FormatException(
                    $"The schema expands to more than {MaxFields} fields; it is refused rather than built.");
            if (++_depth > MaxNestingDepth + 1)
                throw new FormatException(
                    $"The schema nests objects and arrays more than {MaxNestingDepth} levels deep; it is refused rather than built.");
            foreach (var pointer in identity)
                _expanding[pointer] = _expanding.TryGetValue(pointer, out var count) ? count + 1 : 1;
        }

        public void Exit(HashSet<string> identity)
        {
            _depth--;
            foreach (var pointer in identity)
            {
                if (!_expanding.TryGetValue(pointer, out var count)) continue;
                if (count <= 1) _expanding.Remove(pointer);
                else _expanding[pointer] = count - 1;
            }
        }

        /// <summary>
        /// Charges one merge — a document written and read back — against the budget. Every one costs
        /// a copy of the schema it touches, so a document that asks for more than any real form needs is
        /// refused instead of being allowed to spend the server's time.
        /// </summary>
        private void ChargeMerge()
        {
            if (++_mergeSteps > MaxMergeSteps)
                throw new FormatException(
                    $"The schema needs more than {MaxMergeSteps} allOf and $ref merges; it is refused rather than built.");
        }

        /// <summary>Follows a chain of local <c>$ref</c>s to the schema they ultimately name.</summary>
        public JsonElement Resolve(JsonElement schema) => Resolve(schema, followed: null);

        /// <summary>
        /// Follows a chain of local <c>$ref</c>s, recording each definition it passes through. Keywords
        /// written beside a <c>$ref</c> are kept and win over the target's: since 2019-09 a reference
        /// no longer replaces the schema it sits in, and <c>{"$ref": "#/$defs/Address", "title":
        /// "Billing address"}</c> is how pydantic and OpenAPI 3.1 give a shared shape a local label.
        /// </summary>
        /// <param name="schema">The subschema as written.</param>
        /// <param name="followed">Receives a canonical key for every definition passed through.</param>
        /// <param name="keepSiblings">
        /// False to return the bare target. Keeping the siblings costs a merge, which a caller that only
        /// wants to look at the target — is it null, does it pin a const — should not pay for.
        /// </param>
        public JsonElement Resolve(JsonElement schema, ISet<string>? followed, bool keepSiblings = true)
        {
            var current = schema;
            List<JsonElement>? siblings = null;
            for (var depth = 0; depth < MaxRefDepth; depth++)
            {
                if (current.ValueKind != JsonValueKind.Object) break;
                if (!current.TryGetProperty("$ref", out var refElement) || refElement.ValueKind != JsonValueKind.String)
                    break;

                var pointer = refElement.GetString();
                if (pointer is null || !TryResolvePointer(pointer, out var target, out var key)) break;

                followed?.Add(key);
                if (keepSiblings && HasSiblingKeywords(current))
                {
                    ChargeMerge();
                    (siblings ??= []).Add(Without(current, "$ref", "$defs", "definitions"));
                }
                current = target;
            }
            // A reference cycle stops where it is rather than recursing forever.

            if (siblings is null) return current;

            // The schema nearest the field wins: an outer title overrides the one the definition carries.
            // Any allOf on either side has not been folded yet, so it is carried rather than dropped.
            siblings.Reverse();
            return Merge([current, .. siblings], keepAllOf: true);
        }

        /// <summary>
        /// Folds every <c>allOf</c> branch into one schema object. Branches are applied in order and
        /// the outer schema wins for annotations ("this, plus the shared base"); constraints combine
        /// the way <c>allOf</c> means them — every branch applies — so see <see cref="Merge"/>.
        /// </summary>
        /// <remarks>
        /// Each branch is folded before its parent, so a chain of definitions that each extend the next
        /// is a chain of nested calls. A few thousand of them overflowed the stack — which no catch can
        /// stop — so the depth is capped well below anything a real document needs.
        /// </remarks>
        public JsonElement MergeAllOf(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object) return schema;
            if (!schema.TryGetProperty("allOf", out var allOf) || allOf.ValueKind != JsonValueKind.Array)
                return schema;

            _allOfDepth++;
            try
            {
                if (_allOfDepth > MaxAllOfDepth)
                    throw new FormatException(
                        $"The schema nests allOf more than {MaxAllOfDepth} levels deep; it is refused rather than built.");

                var parts = new List<JsonElement>();
                foreach (var raw in allOf.EnumerateArray())
                {
                    ChargeMerge();

                    var pointers = new HashSet<string>(StringComparer.Ordinal);
                    var branch = Resolve(raw, pointers);

                    // A field is every one of its allOf branches, so their definitions are part of what
                    // it is — pydantic v1 and Swashbuckle write a plain reference as `allOf: [{$ref}]`,
                    // and recursion spelled that way has to be recognised as recursion.
                    _identity?.UnionWith(pointers);

                    // A branch that names a definition already expanded as often as it may is the field
                    // containing itself — the recursion the caller leaves out.
                    if (pointers.Any(IsExhausted))
                    {
                        _recursionHit = true;
                        continue;
                    }

                    // A branch that includes the schema it is already part of adds nothing it does not
                    // already say (A and A is A) — and following it was a stack overflow.
                    if (pointers.Any(_merging.Contains)) continue;

                    foreach (var p in pointers) _merging.Add(p);
                    try
                    {
                        parts.Add(MergeAllOf(branch));
                    }
                    finally
                    {
                        foreach (var p in pointers) _merging.Remove(p);
                    }
                }

                parts.Add(schema);
                return Merge(parts);
            }
            finally
            {
                _allOfDepth--;
            }
        }

        /// <summary>
        /// Collapses the one <c>anyOf</c>/<c>oneOf</c> shape that has an unambiguous single-field
        /// meaning: a union whose only other branch is <c>null</c>. That is how most generators spell
        /// "optional string", and it describes exactly one control.
        /// </summary>
        /// <remarks>
        /// A union of genuinely different object shapes is a different question — it needs a selector
        /// and a subform that swaps with it — and guessing a branch would silently bind the user's
        /// answers to the wrong one. Those are left alone rather than half-applied, on the same
        /// principle as a condition that has no JSON form.
        /// </remarks>
        public JsonElement CollapseUnion(JsonElement schema, ISet<string>? identity = null)
        {
            if (schema.ValueKind != JsonValueKind.Object) return schema;
            if (UnionBranches(schema) is not { } union) return schema;

            // A branch list of `const` values (with or without a null) is an enum with labels;
            // ReadChoices reads it where it stands, so the schema is handed back untouched.
            if (ConstBranches(schema) is not null) return schema;

            // Telling a null branch from a real one only needs each reference followed, not each
            // branch's own keywords merged in — and the question is settled by the second real branch,
            // however many more there are.
            JsonElement? chosen = null;
            foreach (var raw in union.Branches.EnumerateArray())
            {
                var target = Resolve(raw, followed: null, keepSiblings: false);
                if (target.ValueKind == JsonValueKind.False || DeclaresOnlyNull(target)) continue;
                if (chosen is not null) return schema;
                chosen = raw;
            }
            if (chosen is not { } single) return schema;

            var pointers = new HashSet<string>(StringComparer.Ordinal);
            var branch = Resolve(single, pointers);
            if (identity is not null)
                foreach (var p in pointers) identity.Add(p);

            // The outer schema wins: its title and description describe the field, not the branch. An
            // allOf on either side is carried for MergeAllOf to fold, not dropped.
            ChargeMerge();
            return Merge([branch, Without(schema, union.Keyword)], keepAllOf: true);
        }

        /// <summary>
        /// The values of an <c>anyOf</c>/<c>oneOf</c> when every branch pins a single <c>const</c> (or
        /// only says "or null") — the shape that means "an enum, with a label per member". Null for
        /// anything else, including a union of object shapes, which describes a choice of subforms
        /// rather than a choice of values.
        /// </summary>
        /// <remarks>
        /// Only <c>const</c> and <c>title</c> are read, so a referenced branch is followed without its
        /// sibling keywords being merged in, and the first branch that pins nothing ends the question.
        /// A union of thousands of described references used to be resolved, merged and materialised
        /// branch by branch, several times for every field that used it.
        /// </remarks>
        public IReadOnlyList<ConstChoice>? ConstBranches(JsonElement schema)
        {
            if (UnionBranches(schema) is not { } union) return null;

            List<ConstChoice>? choices = null;
            foreach (var raw in union.Branches.EnumerateArray())
            {
                var target = Resolve(raw, followed: null, keepSiblings: false);
                if (DeclaresOnlyNull(target)) continue;

                // The branch's own keywords win over the definition it refers to, as they do anywhere.
                if (TryGet(raw, "const", out var value) || TryGet(target, "const", out value))
                    (choices ??= []).Add(new ConstChoice(value, ReadString(raw, "title") ?? ReadString(target, "title")));
                else
                    return null;
            }
            return choices;
        }

        /// <summary>
        /// The union a schema declares. <c>anyOf</c> and <c>oneOf</c> are read in the same order
        /// everywhere, so the decision to collapse and the reading of labelled values never disagree.
        /// </summary>
        private static (string Keyword, JsonElement Branches)? UnionBranches(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object) return null;
            if (schema.TryGetProperty("anyOf", out var any) && any.ValueKind == JsonValueKind.Array) return ("anyOf", any);
            if (schema.TryGetProperty("oneOf", out var one) && one.ValueKind == JsonValueKind.Array) return ("oneOf", one);
            return null;
        }

        /// <summary>Whether a schema holding a <c>$ref</c> also says something of its own.</summary>
        private static bool HasSiblingKeywords(JsonElement schema)
            => schema.EnumerateObject().Any(m => m.Name is not ("$ref" or "$defs" or "definitions" or "$schema"
                or "$id" or "$comment" or "$anchor" or "$dynamicAnchor"));

        /// <summary>Copies a schema without some of its keywords.</summary>
        private static JsonElement Without(JsonElement schema, params string[] keywords)
        {
            var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                foreach (var member in schema.EnumerateObject())
                {
                    if (keywords.Contains(member.Name, StringComparer.Ordinal)) continue;
                    writer.WritePropertyName(member.Name);
                    member.Value.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            return ParseDetached(buffer.ToArray());
        }

        /// <summary>
        /// Folds a sequence of schema objects into one, as <c>allOf</c> means it: every branch applies.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Annotations — <c>title</c>, <c>description</c>, <c>default</c>, <c>x-</c> hints — cannot all
        /// apply, so the later branch wins and the outer schema, which comes last, has the final word.
        /// </para>
        /// <para>
        /// Constraints all apply, so most of them combine rather than overwrite: <c>required</c> and
        /// <c>dependentRequired</c> are unioned; a property — or an array's <c>items</c> — that two
        /// branches both describe becomes the <c>allOf</c> of both descriptions, so a base's
        /// <c>maxLength</c> survives an extension that only relabels the field; the stricter of two
        /// numeric bounds wins, so an outer <c>"minimum": 0</c> cannot loosen a branch's
        /// <c>"minimum": 5</c> (a draft-04 boolean <c>exclusiveMinimum</c> is folded into its own
        /// branch's bound first); two <c>pattern</c>s are both enforced; two <c>enum</c>s keep only the
        /// values both allow; and every branch's <c>if</c>/<c>then</c>/<c>else</c> is kept and applied
        /// separately. Writing several conditionals as an <c>allOf</c> of them is the standard idiom, and
        /// one <c>if</c> slot kept only the last.
        /// </para>
        /// <para>
        /// The rest take the later branch's word, as annotations do: <c>type</c>, <c>format</c>,
        /// <c>const</c>, <c>multipleOf</c> (whose combination is a least common multiple no form needs),
        /// <c>x-enumDisabled</c> and anything unrecognised.
        /// </para>
        /// </remarks>
        /// <param name="branches">The schemas to fold, earliest first.</param>
        /// <param name="keepAllOf">
        /// Whether <c>allOf</c> lists are carried into the result. <see cref="MergeAllOf"/> has already
        /// folded them and drops them; a merge that runs before it (a <c>$ref</c> with siblings, a
        /// collapsed union) must keep them for it to fold.
        /// </param>
        private static JsonElement Merge(IEnumerable<JsonElement> branches, bool keepAllOf = false)
        {
            var allOf = new List<JsonElement>();
            var properties = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
            var propertyOrder = new List<string>();
            var items = new List<JsonElement>();
            var required = new List<string>();
            var scalars = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            var conditionals = new List<(JsonElement If, JsonElement? Then, JsonElement? Else)>();
            var carried = new List<JsonElement>();
            var dependentRequired = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var dependencies = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            var dependencyArrays = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var patterns = new List<string>();
            var enums = new List<JsonElement>();
            var labels = new Dictionary<string, string>(StringComparer.Ordinal);
            var groups = new Dictionary<string, string?>(StringComparer.Ordinal);

            foreach (var branch in branches)
            {
                if (branch.ValueKind != JsonValueKind.Object) continue;

                // Draft-04's boolean exclusiveMinimum qualifies its own branch's minimum. Folded into a
                // numeric exclusive bound here, it can be compared with the other branches' bounds; left
                // as a flag it would end up qualifying whichever minimum won.
                var exclusiveMin = ReadBool(branch, "exclusiveMinimum") == true;
                var exclusiveMax = ReadBool(branch, "exclusiveMaximum") == true;
                RememberLabels(branch, labels, groups);

                JsonElement? ifSchema = null, thenSchema = null, elseSchema = null;
                foreach (var member in branch.EnumerateObject())
                {
                    var value = member.Value;
                    switch (member.Name)
                    {
                        case "allOf":
                            if (keepAllOf && value.ValueKind == JsonValueKind.Array)
                                allOf.AddRange(value.EnumerateArray());
                            break;
                        case "properties" when value.ValueKind == JsonValueKind.Object:
                            foreach (var p in value.EnumerateObject())
                            {
                                if (!properties.TryGetValue(p.Name, out var list))
                                {
                                    properties[p.Name] = list = [];
                                    propertyOrder.Add(p.Name);
                                }
                                list.Add(p.Value);
                            }
                            break;
                        case "items" when value.ValueKind is JsonValueKind.Object or JsonValueKind.True or JsonValueKind.False:
                            items.Add(value);
                            break;
                        case "required" when value.ValueKind == JsonValueKind.Array:
                            AddStrings(value, required);
                            break;
                        case "if":
                            ifSchema = value;
                            break;
                        case "then":
                            thenSchema = value;
                            break;
                        case "else":
                            elseSchema = value;
                            break;
                        case ConditionalsKeyword when value.ValueKind == JsonValueKind.Array:
                            carried.AddRange(value.EnumerateArray());
                            break;
                        case "dependentRequired" when value.ValueKind == JsonValueKind.Object:
                            foreach (var entry in value.EnumerateObject())
                                if (entry.Value.ValueKind == JsonValueKind.Array)
                                    AddStrings(entry.Value, Bucket(dependentRequired, entry.Name));
                            break;
                        case "dependencies" when value.ValueKind == JsonValueKind.Object:
                            foreach (var entry in value.EnumerateObject())
                            {
                                if (entry.Value.ValueKind == JsonValueKind.Array)
                                    AddStrings(entry.Value, Bucket(dependencyArrays, entry.Name));
                                else
                                    dependencies[entry.Name] = entry.Value;
                            }
                            break;
                        case "pattern" when value.ValueKind == JsonValueKind.String:
                            AddPattern(value.GetString()!, patterns);
                            break;
                        case PatternsKeyword when value.ValueKind == JsonValueKind.Array:
                            foreach (var p in value.EnumerateArray())
                                if (p.ValueKind == JsonValueKind.String) AddPattern(p.GetString()!, patterns, primary: false);
                            break;
                        case "enum" when value.ValueKind == JsonValueKind.Array:
                            enums.Add(value);
                            scalars[member.Name] = value;
                            break;
                        case "exclusiveMinimum" or "exclusiveMaximum" when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                            break;
                        case "minimum" when exclusiveMin:
                            Stricter(scalars, "exclusiveMinimum", value, keepLarger: true);
                            break;
                        case "maximum" when exclusiveMax:
                            Stricter(scalars, "exclusiveMaximum", value, keepLarger: false);
                            break;
                        case "minimum" or "exclusiveMinimum" or "minLength" or "minItems":
                            Stricter(scalars, member.Name, value, keepLarger: true);
                            break;
                        case "maximum" or "exclusiveMaximum" or "maxLength" or "maxItems":
                            Stricter(scalars, member.Name, value, keepLarger: false);
                            break;
                        default:
                            scalars[member.Name] = value;
                            break;
                    }
                }

                if (ifSchema is { } condition) conditionals.Add((condition, thenSchema, elseSchema));
            }

            // Two enums allow only what both allow. Labels and groups are positional in each branch, so
            // they are carried by value and laid out again against the values that are left.
            List<JsonElement>? keptEnum = null;
            if (enums.Count > 1)
            {
                var allowed = enums.Take(enums.Count - 1)
                    .Select(e => e.EnumerateArray().Select(EnumKey).ToHashSet(StringComparer.Ordinal))
                    .ToList();
                keptEnum = enums[^1].EnumerateArray().Where(v => allowed.All(a => a.Contains(EnumKey(v)))).ToList();
                scalars.Remove("enum");
                scalars.Remove("enumNames");
                scalars.Remove("x-enumGroups");
            }

            // The merged result is materialised as a new document so the rest of the importer can keep
            // working against plain JsonElement.
            var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                foreach (var (name, value) in scalars)
                {
                    writer.WritePropertyName(name);
                    value.WriteTo(writer);
                }
                if (keptEnum is not null)
                {
                    writer.WritePropertyName("enum");
                    writer.WriteStartArray();
                    foreach (var v in keptEnum) v.WriteTo(writer);
                    writer.WriteEndArray();
                    if (labels.Count > 0)
                    {
                        writer.WritePropertyName("enumNames");
                        WriteStrings(writer, keptEnum.Select(v => labels.TryGetValue(EnumKey(v), out var l) ? l : OptionValue(v) ?? ""));
                    }
                    if (groups.Count > 0)
                    {
                        writer.WritePropertyName("x-enumGroups");
                        writer.WriteStartArray();
                        foreach (var v in keptEnum) writer.WriteStringValue(groups.GetValueOrDefault(EnumKey(v)));
                        writer.WriteEndArray();
                    }
                }
                if (patterns.Count > 0)
                {
                    writer.WriteString("pattern", patterns[0]);
                    if (patterns.Count > 1)
                    {
                        writer.WritePropertyName(PatternsKeyword);
                        WriteStrings(writer, patterns.Skip(1));
                    }
                }
                if (allOf.Count > 0)
                {
                    writer.WritePropertyName("allOf");
                    writer.WriteStartArray();
                    foreach (var branch in allOf) branch.WriteTo(writer);
                    writer.WriteEndArray();
                }
                if (properties.Count > 0)
                {
                    writer.WritePropertyName("properties");
                    writer.WriteStartObject();
                    foreach (var name in propertyOrder)
                    {
                        writer.WritePropertyName(name);
                        WriteCombined(writer, properties[name]);
                    }
                    writer.WriteEndObject();
                }
                if (items.Count > 0)
                {
                    writer.WritePropertyName("items");
                    WriteCombined(writer, items);
                }
                if (required.Count > 0)
                {
                    writer.WritePropertyName("required");
                    WriteStrings(writer, required);
                }
                if (dependentRequired.Count > 0)
                {
                    writer.WritePropertyName("dependentRequired");
                    writer.WriteStartObject();
                    foreach (var (name, names) in dependentRequired)
                    {
                        writer.WritePropertyName(name);
                        WriteStrings(writer, names);
                    }
                    writer.WriteEndObject();
                }
                if (dependencies.Count > 0 || dependencyArrays.Count > 0)
                {
                    writer.WritePropertyName("dependencies");
                    writer.WriteStartObject();
                    foreach (var (name, names) in dependencyArrays)
                    {
                        writer.WritePropertyName(name);
                        WriteStrings(writer, names);
                    }
                    foreach (var (name, value) in dependencies)
                    {
                        if (dependencyArrays.ContainsKey(name)) continue;
                        writer.WritePropertyName(name);
                        value.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                }
                if (conditionals.Count > 0 || carried.Count > 0)
                {
                    writer.WritePropertyName(ConditionalsKeyword);
                    writer.WriteStartArray();
                    foreach (var block in carried) block.WriteTo(writer);
                    foreach (var (condition, then, otherwise) in conditionals)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("if");
                        condition.WriteTo(writer);
                        if (then is { } t)
                        {
                            writer.WritePropertyName("then");
                            t.WriteTo(writer);
                        }
                        if (otherwise is { } e)
                        {
                            writer.WritePropertyName("else");
                            e.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }

            // The document is kept alive by cloning the element out of it.
            return ParseDetached(buffer.ToArray());

            static List<string> Bucket(Dictionary<string, List<string>> map, string key)
                => map.TryGetValue(key, out var list) ? list : map[key] = [];

            static void AddStrings(JsonElement array, List<string> into)
            {
                foreach (var item in array.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s && !into.Contains(s))
                        into.Add(s);
            }

            // The later branch's pattern is the field's own (the outer schema has the last word, as for
            // annotations); the others are enforced beside it. Keeping that order is what makes a field
            // with two patterns export and re-import as the same field.
            static void AddPattern(string pattern, List<string> into, bool primary = true)
            {
                into.Remove(pattern);
                if (primary) into.Insert(0, pattern);
                else into.Add(pattern);
            }

            static void WriteStrings(Utf8JsonWriter writer, IEnumerable<string> values)
            {
                writer.WriteStartArray();
                foreach (var v in values) writer.WriteStringValue(v);
                writer.WriteEndArray();
            }

            // Two numeric bounds of the same kind both apply, so the stricter one stands. A value that
            // is not a number is simply the later word.
            static void Stricter(Dictionary<string, JsonElement> seen, string name, JsonElement incoming, bool keepLarger)
            {
                if (seen.TryGetValue(name, out var existing)
                    && existing.ValueKind == JsonValueKind.Number && incoming.ValueKind == JsonValueKind.Number
                    && existing.TryGetDouble(out var a) && incoming.TryGetDouble(out var b)
                    && (keepLarger ? b <= a : b >= a))
                    return;
                seen[name] = incoming;
            }

            // A value's identity for intersecting enums: 1 and 1.0 are the same member, "1" is not.
            static string EnumKey(JsonElement value) => $"{value.ValueKind}:{OptionValue(value)}";

            static void RememberLabels(JsonElement branch, Dictionary<string, string> labels, Dictionary<string, string?> groups)
            {
                if (!TryGet(branch, "enum", out var values) || values.ValueKind != JsonValueKind.Array) return;
                var names = TryGet(branch, "enumNames", out var n) && n.ValueKind == JsonValueKind.Array ? n : default;
                var groupNames = TryGet(branch, "x-enumGroups", out var g) && g.ValueKind == JsonValueKind.Array ? g : default;

                var i = 0;
                foreach (var value in values.EnumerateArray())
                {
                    var key = EnumKey(value);
                    if (names.ValueKind == JsonValueKind.Array && i < names.GetArrayLength() && names[i].ValueKind == JsonValueKind.String)
                        labels[key] = names[i].GetString()!;
                    if (groupNames.ValueKind == JsonValueKind.Array && i < groupNames.GetArrayLength())
                        groups[key] = groupNames[i].ValueKind == JsonValueKind.String ? groupNames[i].GetString() : null;
                    i++;
                }
            }

            // A property described by several branches is all of those descriptions at once.
            static void WriteCombined(Utf8JsonWriter writer, List<JsonElement> descriptions)
            {
                if (descriptions.Any(d => d.ValueKind == JsonValueKind.False))
                {
                    writer.WriteBooleanValue(false);
                    return;
                }

                var meaningful = descriptions.Where(d => d.ValueKind != JsonValueKind.True).ToList();
                switch (meaningful.Count)
                {
                    case 0:
                        writer.WriteBooleanValue(true);
                        return;
                    case 1:
                        meaningful[0].WriteTo(writer);
                        return;
                }

                writer.WriteStartObject();
                writer.WritePropertyName("allOf");
                writer.WriteStartArray();
                foreach (var d in meaningful) d.WriteTo(writer);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Resolves a local JSON pointer such as <c>#/$defs/Address</c>, and returns a canonical key for
        /// it so that <c>#</c> and <c>#/</c> — or an escaped and an unescaped spelling — are recognised
        /// as the same place.
        /// </summary>
        private bool TryResolvePointer(string pointer, out JsonElement target, out string key)
        {
            target = default;
            key = string.Empty;
            if (!pointer.StartsWith('#')) return false; // remote references are out of scope

            // A pointer in a URI fragment is percent-encoded (RFC 6901 §6): "#/$defs/My%20Type" names
            // the definition "My Type", and reading it literally named nothing.
            var fragment = Uri.UnescapeDataString(pointer[1..]);
            var segments = fragment.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var current = _root;
            foreach (var rawSegment in segments)
            {
                var segment = rawSegment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var next))
                {
                    current = next;
                }
                else if (current.ValueKind == JsonValueKind.Array
                         && int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                         && index >= 0 && index < current.GetArrayLength())
                {
                    current = current[index];
                }
                else
                {
                    return false;
                }
            }

            target = current;
            key = "#" + string.Concat(segments.Select(s => "/" + s));
            return true;
        }
    }
}

/// <summary>
/// A further <c>pattern</c> the value must also match — what an <c>allOf</c> of two patterns means.
/// Keyless, so it sits beside the field's own <see cref="BormPatternRule"/> rather than
/// replacing it, and checks exactly what that rule checks (each tag of a tag list included).
/// </summary>
internal sealed class BormAdditionalPatternRule(string pattern) : IBormValidationRule
{
    private readonly BormPatternRule _rule = new(pattern);

    /// <summary>The pattern.</summary>
    public string Pattern => pattern;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx) => _rule.ValidateAsync(ctx);
}

/// <summary>
/// Enforces JSON Schema's <c>"format": "uuid"</c>. A rule of its own rather than a
/// <see cref="BormPatternRule"/>, because a document can say both — a UUID that must also match
/// the document's own <c>pattern</c> — and two pattern rules share one key, so the second replaced the
/// first.
/// </summary>
internal sealed class BormUuidRule(string? message = null) : IBormValidationRule
{
    private static readonly Regex? Uuid = BormRegex.Create(BormJsonSchemaImporter.UuidPattern);

    /// <inheritdoc />
    public string Key => "uuid";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        if (ctx.Value is not string { Length: > 0 } s) return new(BormRuleResult.Success());
        return BormRegex.IsMatch(Uuid, s) == false
            ? new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.Uuid)))
            : new(BormRuleResult.Success());
    }
}
