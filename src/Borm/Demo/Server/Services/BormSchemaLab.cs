using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using Bit.Borm.Demo.Server.Dtos;

namespace Bit.Borm.Demo.Server.Services;

/// <summary>
/// Runs a JSON Schema document through the library itself, so "what form does this become, what is
/// wrong with it, and what comes back out?" is answered by the code that decides it at runtime rather
/// than by a reading of the document.
/// <para>
/// Three things do the work, and none of them is this class. <see cref="BormJsonSchemaImporter.TryImport"/>
/// reads the document exactly as an application's "paste your schema here" editor would - so a document
/// this tool accepts is one the application accepts, and the error it reports is the one the
/// application gets. <see cref="BormDefinition.Validate"/> checks the form it became, and
/// <see cref="BormJsonSchemaExporter.Export"/> writes it back out, which is imported once more to see
/// whether it survives the trip. What this class adds is only what those three do not report - a
/// pattern .NET cannot compile, a condition whose operator names nothing, an extension Borm does not
/// read, a reference to nowhere - each of which the importer passes over in silence, and the notes
/// that say what to do about every finding.
/// </para>
/// </summary>
public static partial class BormSchemaLab
{
    /// <summary>
    /// The largest document this tool reads. The schema arrives as a tool argument, and a document
    /// past this size is past what a question about one form needs.
    /// </summary>
    public const int MaxInputBytes = 2 * 1024 * 1024;

    /// <summary>The most items one note lists before it says how many more there are.</summary>
    private const int MaxListed = 8;

    /// <summary>The most choices one field lists; the rest are counted.</summary>
    private const int MaxOptionsListed = 20;

    /// <summary>The extensions whose value is a condition, which the importer reads with BormConditionJson - or drops.</summary>
    private static readonly string[] _conditionExtensions = ["x-visibleWhen", "x-disabledWhen", "x-readOnlyWhen", "x-requiredWhen"];

    /// <summary>Keywords whose value is a map of names to subschemas.</summary>
    private static readonly string[] _schemaMaps = ["properties", "$defs", "definitions", "patternProperties", "dependentSchemas"];

    /// <summary>Keywords whose value is one subschema.</summary>
    private static readonly string[] _schemaValues =
    [
        "items", "additionalItems", "additionalProperties", "if", "then", "else", "not", "contains", "propertyNames",
        "unevaluatedItems", "unevaluatedProperties"
    ];

    /// <summary>Keywords whose value is a list of subschemas.</summary>
    private static readonly string[] _schemaLists = ["allOf", "anyOf", "oneOf", "prefixItems", "items"];

    /// <summary>
    /// How the sample is written: the exporter's own indented output, read back and written again with
    /// the server's line endings left out, so it is the same text on every operating system.
    /// </summary>
    private static readonly JsonWriterOptions _write = new() { Indented = true, NewLine = "\n" };

    private static readonly Lazy<string> _sample = new(BuildSample);
    private static readonly Lazy<string[]> _extensions = new(ReadExtensions);

    /// <summary>A small wizard built with the library's builder and written by its exporter.</summary>
    public static string Sample => _sample.Value;

    /// <summary>
    /// Every <c>x-</c> extension Bit.Borm reads or writes, found where the compiler put them: as string
    /// literals in the shipped assembly. The importer looks each one up by name, so a literal is the
    /// whole of what "Borm reads x-something" means - and nothing has to be kept in step with it.
    /// </summary>
    public static string[] Extensions => _extensions.Value;

    public static BormSchemaReportDto Inspect(string? schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return new BormSchemaReportDto
            {
                Valid = false,
                InputBytes = 0,
                Sample = Sample,
                Notes =
                [
                    "No schema was passed, so nothing was inspected. Sample is a two-step support-request wizard - a conditional, " +
                    "conditionally required field, a select, a bounded number shown for one answer only, a tag list - built with " +
                    "BormBuilder and written by BormJsonSchemaExporter.Export; pass it, or your own JSON Schema document, as schema " +
                    "to see the report."
                ]
            };
        }

        var inputBytes = Encoding.UTF8.GetByteCount(schema);

        if (inputBytes > MaxInputBytes)
        {
            return Invalid(inputBytes, $"The schema is {inputBytes} bytes; this tool reads documents of up to {MaxInputBytes} bytes.");
        }

        // ---- Import it the way an application would --------------------------------

        BormDefinition definition;

        try
        {
            if (BormJsonSchemaImporter.TryImport(schema, out definition, out var error) is false)
            {
                return Invalid(inputBytes, error ?? "The importer refused the document without saying why.",
                    "This is BormJsonSchemaImporter.TryImport's own message - what an application calling it gets for the same text. " +
                    "It refuses malformed JSON (no comments, no trailing commas), a document that is not a schema, and one past the " +
                    "budget the guide's 'Untrusted documents' section lists.");
            }
        }
        catch (Exception exception)
        {
            // TryImport exists to turn a bad document into false and a message; one that gets past it
            // crashes the very "paste your schema" editor it is meant for.
            return Invalid(inputBytes, $"{exception.GetType().Name}: {exception.Message}",
                "BormJsonSchemaImporter.TryImport threw instead of returning false, so an application calling it would crash on this " +
                "document. That is a bug in the library, not in the document.");
        }

        var notes = new List<string>();

        var fields = Walk(definition.Fields, string.Empty).ToArray();
        var steps = StepOf(definition);

        var report = new BormSchemaReportDto
        {
            Valid = true,
            InputBytes = inputBytes,
            Title = definition.Title,
            Description = definition.Description,
            Columns = definition.Columns > 1 ? definition.Columns : null,
            FieldCount = fields.Length,
            FieldTypes = [.. fields.GroupBy(field => field.Field.Type.ToString(), StringComparer.Ordinal)
                                   .Select(group => new BormCountDto { Name = group.Key, Count = group.Count() })
                                   .OrderByDescending(count => count.Count)
                                   .ThenBy(count => count.Name, StringComparer.Ordinal)],
            Fields = [.. fields.Select(field => DescribeField(field.Path, field.Field, steps.GetValueOrDefault(field.Path)))],
            Steps = definition.IsWizard
                ? [.. definition.Steps.Select(step => new BormSchemaStepDto
                  {
                      Id = step.Id,
                      Title = step.Title,
                      Fields = [.. step.Fields],
                      VisibleWhen = Condition(step.VisibleWhen)
                  })]
                : null,
            Notes = []
        };

        // ---- Check it, as the library checks a schema ------------------------------

        var diagnostics = definition.Validate();

        report = report with
        {
            Diagnostics = [.. diagnostics.Select(diagnostic => new BormSchemaDiagnosticDto
            {
                Severity = diagnostic.Severity.ToString(),
                Path = diagnostic.Path,
                Message = diagnostic.Message
            })]
        };

        if (fields.Length == 0)
        {
            notes.Add("The document imports to a form with no fields at all: it has no properties the importer could turn into one " +
                      "(true, {} and a document of only $defs all describe an empty form).");
        }

        if (diagnostics.Count > 0)
        {
            var errors = diagnostics.Count(diagnostic => diagnostic.Severity == BormSchemaDiagnosticSeverity.Error);

            notes.Add($"BormDefinition.Validate() reports {errors} error(s) and {diagnostics.Count - errors} warning(s) for the form this " +
                      "document becomes, each saying what to change. An error means the form will throw, bind wrongly or silently lose " +
                      "data; a warning, that it renders but probably not as meant. The library never runs Validate() by itself - a schema " +
                      "being edited has every right to be incomplete - so call it wherever documents come from (a test, a schema editor).");
        }

        // ---- What the importer passes over in silence ------------------------------

        CheckPatterns(fields, notes);
        CheckDocument(schema, definition, notes);
        CheckRenderers(fields, notes);

        // ---- Write it back out, and read that once more -----------------------------

        var (export, roundTrips) = RoundTrip(definition, notes);

        notes.Add("Load the same document in an app with BormJsonSchemaImporter.TryImport(json, out var definition, out var error) - it answers " +
                  "a bad or oversized document with false and a message rather than an exception - check it with definition.Validate(), and " +
                  "render it with <BormView Definition=\"definition\" Data=\"answers\" />, where answers is a Dictionary<string, object?> the " +
                  "values are written into (or Model=\"...\" for a typed model whose property names match).");

        return report with { Export = export, RoundTrips = roundTrips, Notes = [.. notes] };
    }

    private static BormSchemaReportDto Invalid(int inputBytes, string error, string? note = null) => new()
    {
        Valid = false,
        Error = error,
        InputBytes = inputBytes,
        Notes = note is null ? [] : [note]
    };

    // ---- The form ---------------------------------------------------------------------

    /// <summary>
    /// Every field in the order it renders, depth first, each with the path it binds to. A repeater's
    /// row is written <c>name[]</c>: every row shares one item template, so "lines[].product" is the
    /// product field of every row. An object row is not listed itself - it is the row, and its fields
    /// are - while a scalar row is, since its rules are the ones every entry is checked against.
    /// </summary>
    private static IEnumerable<(string Path, BormFieldDefinition Field)> Walk(IEnumerable<BormFieldDefinition> fields, string prefix)
    {
        foreach (var field in fields.OrderBy(field => field.Order))
        {
            var path = prefix.Length == 0 ? field.Name : $"{prefix}.{field.Name}";

            yield return (path, field);

            if (field.Type == BormFieldType.Object)
            {
                foreach (var child in Walk(field.Children, path)) yield return child;
            }
            else if (field.ItemTemplate is { } template)
            {
                var row = $"{path}[]";

                if (template.Type == BormFieldType.Object)
                {
                    foreach (var child in Walk(template.Children, row)) yield return child;
                }
                else
                {
                    yield return (row, template);
                }
            }
        }
    }

    /// <summary>The wizard step each top-level field is shown on: the first step that lists it, as the wizard itself finds it.</summary>
    private static Dictionary<string, string> StepOf(BormDefinition definition)
    {
        var steps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in definition.Steps)
        {
            foreach (var name in step.Fields) steps.TryAdd(name, step.Id);
        }

        return steps;
    }

    private static BormSchemaFieldDto DescribeField(string path, BormFieldDefinition field, string? step)
    {
        var options = field.Options.Select(option => option.Disabled ? $"{option.Value} = {option.Label} (disabled)" : $"{option.Value} = {option.Label}").ToArray();

        return new BormSchemaFieldDto
        {
            Path = path,
            Type = field.Type.ToString(),
            ValueType = field.ValueType is null ? null : BormApiCatalog.FriendlyName(field.ValueType),
            Label = field.Label,
            Required = field.Required,
            ReadOnly = field.ReadOnly ? true : null,
            Step = step,
            Group = field.Group,
            Default = Json(field.DefaultValue),
            Rules = field.Validators.Count == 0 ? null : [.. field.Validators.Select(rule => DescribeRule(rule, field))],
            VisibleWhen = Condition(field.VisibleWhen),
            RequiredWhen = Condition(field.RequiredWhen),
            DisabledWhen = Condition(field.DisabledWhen),
            ReadOnlyWhen = Condition(field.ReadOnlyWhen),
            ClearOnHide = field.ClearOnHide ? true : null,
            Options = options.Length == 0 ? null : [.. options.Take(MaxOptionsListed)],
            OptionCount = options.Length > MaxOptionsListed ? options.Length : null,
            Renderer = field.CustomRenderer
        };
    }

    /// <summary>
    /// A rule as one short phrase - its kind and the numbers that matter. The bounds come off the rule
    /// itself where it carries them, so what is reported is what the rule enforces rather than what the
    /// field's attributes say.
    /// </summary>
    private static string DescribeRule(IBormValidationRule rule, BormFieldDefinition field) => rule switch
    {
        BormRequiredRule => "required",
        BormMinLengthRule minLength => $"minLength {minLength.Min}",
        BormMaxLengthRule maxLength => $"maxLength {maxLength.Max}",
        BormRangeRule range => $"range {Bounds(range, field.Type)}",
        BormPatternRule pattern => pattern.IsPatternValid ? $"pattern {pattern.Pattern}" : $"pattern {pattern.Pattern} (does not compile - never enforced)",
        BormMultipleOfRule multipleOf => $"multipleOf {multipleOf.Factor.ToString(CultureInfo.InvariantCulture)}",
        BormCollectionSizeRule => $"items {field.MinItems?.ToString(CultureInfo.InvariantCulture) ?? "0"}..{field.MaxItems?.ToString(CultureInfo.InvariantCulture) ?? "any"}",
        BormCompareRule compare => $"compare with '{compare.OtherPath}'",
        BormFileRule => $"file{(field.MaxFileSize is { } size ? $" up to {size.ToString(CultureInfo.InvariantCulture)} bytes" : null)}{(field.Accept is { } accept ? $" of {accept}" : null)}",
        BormDelegateRule or BormAsyncDelegateRule or BormConditionalRule => "custom rule (code)",
        _ => Generic(rule)
    };

    /// <summary>
    /// A rule this class has no case for - one the importer keeps internal, say - by its key, or by a
    /// pattern it carries, or by its type's own name.
    /// </summary>
    private static string Generic(IBormValidationRule rule)
    {
        var type = rule.GetType();

        if (type.GetProperty("Pattern", BindingFlags.Public | BindingFlags.Instance)?.GetValue(rule) is string pattern) return $"pattern {pattern}";

        if (rule.Key is { } key) return key;

        var name = type.Name.StartsWith("Borm", StringComparison.Ordinal) ? type.Name[4..] : type.Name;
        if (name.EndsWith("Rule", StringComparison.Ordinal)) name = name[..^4];

        return name.Length == 0 ? type.Name : $"{char.ToLowerInvariant(name[0])}{name[1..]}";
    }

    /// <summary>
    /// The bounds of a range as a reader would write them, inclusive or exclusive as the rule is. A
    /// date's bound is held as the number that stands for it, so it is turned back into the date.
    /// </summary>
    private static string Bounds(BormRangeRule range, BormFieldType type)
    {
        var parts = new List<string>(2);

        if (range.Min is { } min) parts.Add($"{(range.MinExclusive ? ">" : ">=")} {Bound(min, type)}");
        if (range.Max is { } max) parts.Add($"{(range.MaxExclusive ? "<" : "<=")} {Bound(max, type)}");

        return parts.Count == 0 ? "(unbounded)" : string.Join(" and ", parts);
    }

    private static string Bound(double value, BormFieldType type)
    {
        try
        {
            return type switch
            {
                BormFieldType.Date => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                BormFieldType.DateTime => DateTime.FromOADate(value).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                BormFieldType.Time => DateTime.FromOADate(value).ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                _ => value.ToString(CultureInfo.InvariantCulture)
            };
        }
        catch (ArgumentException)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A condition in the form an <c>x-visibleWhen</c> takes, written by the library's own
    /// BormConditionJson - so what is reported is exactly what the document would have to say to get
    /// it, including what <c>if</c>/<c>then</c>/<c>else</c> and <c>dependentRequired</c> turned into.
    /// </summary>
    private static string? Condition(IBormCondition? condition)
    {
        if (condition is null) return null;

        if (BormConditionJson.CanWrite(condition) is false)
        {
            return $"(code, reading {string.Join(", ", condition.Dependencies.Select(dependency => $"'{dependency}'"))})";
        }

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            BormConditionJson.TryWrite(writer, condition);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string? Json(object? value)
    {
        if (value is null) return null;

        try
        {
            return JsonSerializer.Serialize(value, value.GetType());
        }
        catch (Exception)
        {
            // A default the serializer cannot write is still a default; its text is better than nothing.
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }

    // ---- What the importer does not report ------------------------------------------------

    /// <summary>
    /// A pattern .NET cannot compile becomes a rule that never fails: BormPatternRule treats it as no
    /// constraint at all, so the document imports and the form renders - and accepts anything - with
    /// no diagnostic anywhere. Validate() does not look at patterns.
    /// </summary>
    private static void CheckPatterns((string Path, BormFieldDefinition Field)[] fields, List<string> notes)
    {
        var broken = fields.SelectMany(field => field.Field.Validators.OfType<BormPatternRule>()
                                                    .Where(rule => rule.IsPatternValid is false)
                                                    .Select(rule => $"'{field.Path}' ({rule.Pattern})"))
                           .ToArray();

        if (broken.Length > 0)
        {
            notes.Add($"The pattern of {List(broken)} is not a regular expression .NET can compile, so the rule is treated as no constraint: " +
                      "the field accepts anything, and neither the importer nor Validate() says so. JSON Schema patterns are ECMA-262; most " +
                      "translate as written - fix the syntax, and test the pattern with Regex in C#.");
        }
    }

    /// <summary>
    /// A field asking for a custom renderer throws when it renders unless the key is registered with
    /// AddBorm - in every container that renders the form. The document cannot know what the
    /// application registered, so this is a reminder rather than a finding.
    /// </summary>
    private static void CheckRenderers((string Path, BormFieldDefinition Field)[] fields, List<string> notes)
    {
        var keyed = fields.Where(field => field.Field.CustomRenderer is not null)
                          .Select(field => $"'{field.Path}' ('{field.Field.CustomRenderer}')")
                          .ToArray();

        if (keyed.Length > 0)
        {
            notes.Add($"Field(s) {List(keyed)} ask the renderer registry for a custom renderer (x-renderer). Rendering throws, naming the key, " +
                      "unless the app registers it - builder.Services.AddBorm(registry => registry.RegisterCustom<MyInput>(\"key\")) in every " +
                      "container that renders the form - with a component that inherits BormInputBase.");
        }
    }

    /// <summary>
    /// Walks the document itself for what the importer drops without a word: an extension it does not
    /// read, a condition it cannot read, a reference that resolves to nothing, and a top-level property
    /// that became no field.
    /// </summary>
    private static void CheckDocument(string schema, BormDefinition definition, List<string> notes)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(schema);
        }
        catch (JsonException)
        {
            // The importer has just read this very text, so this does not happen; if it did, the
            // importer's own answer above is the one that counts.
            return;
        }

        using (document)
        {
            var root = document.RootElement;

            var unknown = new List<string>();
            var misspelled = new List<string>();
            var dropped = new List<string>();
            var dangling = new List<string>();
            var remote = new List<string>();

            var known = Extensions.ToHashSet(StringComparer.Ordinal);
            var visited = 0;

            Visit(root, string.Empty);

            void Visit(JsonElement node, string pointer)
            {
                // A document past the importer's own nesting budget has already been refused; this only
                // guards the walk against a pathological one that was not.
                if (node.ValueKind != JsonValueKind.Object || ++visited > 100_000) return;

                foreach (var property in node.EnumerateObject())
                {
                    var name = property.Name;
                    var at = $"{pointer}/{Escape(name)}";

                    if (name.StartsWith("x-", StringComparison.Ordinal))
                    {
                        if (known.Contains(name) is false)
                        {
                            var close = Extensions.FirstOrDefault(extension => string.Equals(extension, name, StringComparison.OrdinalIgnoreCase));
                            unknown.Add(close is null ? $"'{name}' at {Where(pointer)}" : $"'{name}' at {Where(pointer)} (did you mean '{close}'?)");
                        }
                        else if (_conditionExtensions.Contains(name, StringComparer.Ordinal) && BormConditionJson.Read(property.Value) is null)
                        {
                            dropped.Add($"{name} at {Where(pointer)}");
                        }
                        else if (name == "x-steps" && property.Value.ValueKind == JsonValueKind.Array)
                        {
                            var index = 0;
                            foreach (var step in property.Value.EnumerateArray())
                            {
                                if (step.ValueKind == JsonValueKind.Object && step.TryGetProperty("visibleWhen", out var condition) && BormConditionJson.Read(condition) is null)
                                {
                                    dropped.Add($"the visibleWhen of x-steps[{index}]");
                                }

                                index++;
                            }
                        }

                        continue;
                    }

                    // An extension written without its x- is a keyword JSON Schema does not have and Borm
                    // does not read; the one with the x- is almost certainly what was meant.
                    if (name != "enum" && known.Contains($"x-{name}"))
                    {
                        misspelled.Add($"'{name}' at {Where(pointer)} (did you mean 'x-{name}'?)");
                    }

                    if (name == "$ref" && property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } reference)
                    {
                        if (reference.StartsWith('#'))
                        {
                            if (Resolve(root, reference) is false) dangling.Add($"'{reference}' at {Where(pointer)}");
                        }
                        else
                        {
                            remote.Add($"'{reference}' at {Where(pointer)}");
                        }
                    }

                    if (_schemaMaps.Contains(name, StringComparer.Ordinal) && property.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var entry in property.Value.EnumerateObject()) Visit(entry.Value, $"{at}/{Escape(entry.Name)}");
                    }
                    else if (name == "dependencies" && property.Value.ValueKind == JsonValueKind.Object)
                    {
                        // Draft-07's dependencies holds either a list of names or a subschema per property.
                        foreach (var entry in property.Value.EnumerateObject()) Visit(entry.Value, $"{at}/{Escape(entry.Name)}");
                    }
                    else if (_schemaLists.Contains(name, StringComparer.Ordinal) && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var index = 0;
                        foreach (var item in property.Value.EnumerateArray()) Visit(item, $"{at}/{index++}");
                    }
                    else if (_schemaValues.Contains(name, StringComparer.Ordinal))
                    {
                        Visit(property.Value, at);
                    }
                }
            }

            if (unknown.Count > 0)
            {
                notes.Add($"Extension(s) {List(unknown)} are not ones Borm reads, so the importer ignores them. The extensions it reads are: " +
                          $"{string.Join(", ", Extensions)} - GetBormGuideSection(heading: \"Extensions and export\") says what each one means.");
            }

            if (misspelled.Count > 0)
            {
                notes.Add($"Keyword(s) {List(misspelled)} are neither JSON Schema nor anything Borm reads, so they are ignored; the extension " +
                          "with the x- prefix is.");
            }

            if (dropped.Count > 0)
            {
                notes.Add($"Condition(s) {List(dropped)} could not be read, so the importer dropped each as a whole and its field or step behaves " +
                          "as if it had none - always shown, never required by it. A condition is {\"field\": ..., \"op\": ..., \"value\": ...} or " +
                          "{\"all\": [...]} / {\"any\": [...]}; op must name a BormConditionOperator (Equals, NotEquals, GreaterThan, In, " +
                          "IsTrue, IsEmpty, ...) in any case, and a group with one unreadable member is dropped entirely.");
            }

            if (dangling.Count > 0)
            {
                notes.Add($"Reference(s) {List(dangling)} point at nothing in the document. The importer does not fail on them: the field " +
                          "they describe imports as an untyped text box. Point each $ref at a definition under $defs or definitions.");
            }

            if (remote.Count > 0)
            {
                notes.Add($"Reference(s) {List(remote)} point outside the document. The importer only follows references into the document " +
                          "itself, so each of those fields imports as an untyped text box; bundle the referenced schemas into $defs first.");
            }

            DroppedProperties(root, definition, notes);
        }
    }

    /// <summary>
    /// The top-level properties the document declares that became no field. A <c>false</c> subschema is
    /// one on purpose; anything else is a property the form cannot collect, which a submit then never
    /// writes - the kind of loss nobody notices until the data is missing.
    /// </summary>
    private static void DroppedProperties(JsonElement root, BormDefinition definition, List<string> notes)
    {
        if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("properties", out var properties) is false || properties.ValueKind != JsonValueKind.Object) return;

        var fields = definition.Fields.Select(field => field.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = properties.EnumerateObject()
                                .Where(property => fields.Contains(property.Name) is false)
                                .Select(property => property.Value.ValueKind == JsonValueKind.False ? $"'{property.Name}' (false)" : $"'{property.Name}'")
                                .ToArray();

        if (missing.Length > 0)
        {
            notes.Add($"Top-level propert(ies) {List(missing)} of the document became no field of the form. A false subschema means \"never " +
                      "valid\" and is left out on purpose; for anything else the form has no control, so a submit never writes the value.");
        }
    }

    /// <summary>Whether a local reference (<c>#</c>, <c>#/$defs/Address</c>) names something in the document.</summary>
    private static bool Resolve(JsonElement root, string reference)
    {
        var pointer = Uri.UnescapeDataString(reference[1..]);
        if (pointer.Length == 0) return true;

        // An anchor ("#address") names a subschema by its $anchor rather than by its place.
        if (pointer.StartsWith('/') is false) return true;

        var current = root;

        foreach (var token in pointer[1..].Split('/'))
        {
            var name = token.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(name, out var next))
            {
                current = next;
                continue;
            }

            if (current.ValueKind == JsonValueKind.Array && int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index < current.GetArrayLength())
            {
                current = current[index];
                continue;
            }

            return false;
        }

        return true;
    }

    private static string Escape(string name) => name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    private static string Where(string pointer) => pointer.Length == 0 ? "the root" : pointer;

    // ---- The round trip -------------------------------------------------------------------

    /// <summary>
    /// The document the exporter writes for the imported form, and whether importing that again gives
    /// the same form. A form that does not survive the trip is one a schema editor built on the two
    /// would quietly change every time it saved - which is a finding about the library, not the document.
    /// </summary>
    private static (string? Export, bool? RoundTrips) RoundTrip(BormDefinition definition, List<string> notes)
    {
        string export;

        try
        {
            export = BormJsonSchemaExporter.Export(definition);
        }
        catch (Exception exception)
        {
            notes.Add($"BormJsonSchemaExporter.Export threw for the imported form - {exception.GetType().Name}: {exception.Message} That is a bug in the library, not in the document.");
            return (null, false);
        }

        // The exporter writes with the platform's line endings; one text on every operating system reads better.
        export = export.Replace("\r\n", "\n", StringComparison.Ordinal);

        bool imported;
        BormDefinition again;
        string? error;

        try
        {
            imported = BormJsonSchemaImporter.TryImport(export, out again, out error);
        }
        catch (Exception exception)
        {
            notes.Add($"The exported document makes TryImport throw - {exception.GetType().Name}: {exception.Message} That is a bug in the library.");
            return (export, false);
        }

        if (imported is false)
        {
            notes.Add($"The exported document does not import again: {error} That is a bug in the library, not in the document.");
            return (export, false);
        }

        var before = Signature(definition);
        var after = Signature(again);

        var lost = before.Keys.Where(key => after.ContainsKey(key) is false).ToArray();
        var gained = after.Keys.Where(key => before.ContainsKey(key) is false).ToArray();
        var changed = before.Where(pair => after.TryGetValue(pair.Key, out var other) && other != pair.Value)
                            .Select(pair => $"{pair.Key}: {pair.Value} became {after[pair.Key]}")
                            .ToArray();

        if (lost.Length + gained.Length + changed.Length == 0) return (export, true);

        var parts = new List<string>();
        if (lost.Length > 0) parts.Add($"lost {List(lost)}");
        if (gained.Length > 0) parts.Add($"gained {List(gained)}");
        if (changed.Length > 0) parts.Add($"changed {List(changed)}");

        notes.Add($"The form does not survive Export and a second import unchanged - it {string.Join("; ", parts)}. A schema editor that saves " +
                  "through BormJsonSchemaExporter would alter the form on every save; that is a finding about the library worth reporting.");

        return (export, false);
    }

    /// <summary>
    /// What a form is made of, field by field and step by step, in the terms a round trip has to keep:
    /// the path, the type, requiredness, the rules and the conditions, and the choices. Labels and the
    /// other presentation hints are left out - an exporter is free to spell them as it likes.
    /// </summary>
    private static Dictionary<string, string> Signature(BormDefinition definition)
    {
        var signature = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (path, field) in Walk(definition.Fields, string.Empty))
        {
            var described = DescribeField(path, field, step: null);

            signature.TryAdd(path, string.Join(" | ",
                described.Type,
                described.Required ? "required" : "optional",
                string.Join(", ", (described.Rules ?? []).Order(StringComparer.Ordinal)),
                described.VisibleWhen, described.RequiredWhen, described.DisabledWhen, described.ReadOnlyWhen,
                string.Join(", ", field.Options.Select(option => option.Value))));
        }

        foreach (var step in definition.Steps)
        {
            signature.TryAdd($"step '{step.Id}'", $"{string.Join(", ", step.Fields)} | {Condition(step.VisibleWhen)}");
        }

        return signature;
    }

    // ---- Helpers ----------------------------------------------------------------------------

    private static string List(IReadOnlyCollection<string> items)
    {
        var shown = string.Join(", ", items.Take(MaxListed));

        return items.Count > MaxListed ? $"{shown} and {items.Count - MaxListed} more" : shown;
    }

    /// <summary>
    /// Every <c>x-</c> literal in the library's IL. A razor file, a lambda and a state machine all
    /// compile into methods of some type of the assembly, nested and internal ones included, so the
    /// whole assembly is read - the importer's helpers are private nested types.
    /// </summary>
    private static string[] ReadExtensions()
    {
        const BindingFlags Everything = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        const byte LoadString = 0x72;
        const byte StringTable = 0x70;

        var found = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var type in typeof(BormJsonSchemaImporter).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(Everything).Cast<MethodBase>().Concat(type.GetConstructors(Everything)))
            {
                byte[]? il;

                try
                {
                    il = method.GetMethodBody()?.GetILAsByteArray();
                }
                catch (Exception)
                {
                    // A method without a readable body has no literal to find.
                    continue;
                }

                if (il is null) continue;

                // A string token's top byte names the user-string heap, which is what tells an ldstr
                // from a byte of some other instruction's operand that happens to be 0x72.
                for (var i = 0; i + 4 < il.Length; i++)
                {
                    if (il[i] != LoadString || il[i + 4] != StringTable) continue;

                    string literal;

                    try
                    {
                        literal = type.Module.ResolveString(BitConverter.ToInt32(il, i + 1));
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    if (ExtensionRegex().IsMatch(literal)) found.Add(literal);
                }
            }
        }

        return [.. found];
    }

    // ---- The sample -----------------------------------------------------------------------

    /// <summary>
    /// The sample: a support request in two steps - one of each thing a typical form holds, a field
    /// that appears and becomes required on a checkbox, a choice, a bounded number shown for one answer
    /// only, a long text with both bounds and a tag list - built with the library's builder and written
    /// by its exporter, so the format is the one Borm itself produces. It is built once per process, so
    /// every call answers with the same text.
    /// </summary>
    private static string BuildSample()
    {
        var definition = BormBuilder.Create()
            .Title("Support request")
            .Description("Tell us what happened - we answer within one working day.")
            .Text("name", f => f.Label("Your name").Required().MaxLength(80))
            .Text("email", f => f.AsEmail().Required())
            .Checkbox("forCompany", f => f.Label("I am writing on behalf of a company"))
            .Text("company", f => f
                .VisibleWhen("forCompany", BormConditionOperator.IsTrue)
                .RequiredWhen("forCompany", BormConditionOperator.IsTrue)
                .ClearOnHide())
            .Select("topic", f => f.Required().Options(("bug", "Something is broken"), ("idea", "An idea"), ("billing", "Billing")))
            .Integer("severity", f => f
                .Label("How bad is it? (1-5)")
                .Range(1, 5)
                .VisibleWhen("topic", BormConditionOperator.Equals, "bug"))
            .TextArea("details", f => f.Required().MinLength(20).MaxLength(2000))
            .Tags("labels", f => f.AsTags(max: 5))
            .Step("you", s => s.Title("About you").Fields("name", "email", "forCompany", "company"))
            .Step("request", s => s.Title("Your request").Fields("topic", "severity", "details", "labels"))
            .Build();

        using var exported = JsonDocument.Parse(BormJsonSchemaExporter.Export(definition));
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, _write))
        {
            exported.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [GeneratedRegex("^x-[A-Za-z]+$")]
    private static partial Regex ExtensionRegex();
}
