using System.Globalization;

namespace Bit.Borm;

/// <summary>
/// One field as the designer edits it: what its properties panel shows, in the shape a person thinks about a
/// field - "required", "show when", "one option per line" - rather than the rules and conditions it becomes.
/// </summary>
/// <remarks>
/// Built into a <see cref="BormFieldDefinition"/> on every change, through the same builder an application
/// uses. A field read from an existing schema remembers the definition it came from, and what the designer
/// has no place for - an options provider, a condition or a rule written in code, renderer hints - is kept
/// from it as it was.
/// </remarks>
internal sealed class BormDesignerField
{
    /// <summary>The kinds of field the designer offers, each with the field type and the hint it becomes.</summary>
    internal static readonly (string Kind, BormFieldType Type)[] Kinds =
    [
        ("text", BormFieldType.Text), ("textarea", BormFieldType.TextArea), ("markdown", BormFieldType.Markdown),
        ("email", BormFieldType.Email), ("password", BormFieldType.Password), ("url", BormFieldType.Url),
        ("tel", BormFieldType.Tel), ("number", BormFieldType.Number), ("integer", BormFieldType.Integer),
        ("range", BormFieldType.Range), ("date", BormFieldType.Date), ("datetime", BormFieldType.DateTime),
        ("time", BormFieldType.Time), ("month", BormFieldType.Month), ("week", BormFieldType.Week),
        ("checkbox", BormFieldType.Checkbox), ("switch", BormFieldType.Checkbox), ("select", BormFieldType.Select),
        ("radio", BormFieldType.Radio), ("combobox", BormFieldType.Combobox), ("multiselect", BormFieldType.MultiSelect),
        ("tags", BormFieldType.Tags), ("color", BormFieldType.Color), ("file", BormFieldType.File),
        ("signature", BormFieldType.Signature), ("static", BormFieldType.Static), ("search", BormFieldType.Search)
    ];

    /// <summary>
    /// The kinds a schema can hold that the designer keeps but does not offer: what makes one what it is - its
    /// children, its rows, its renderer - has no place in the properties panel, so it is kept as it was, its type
    /// with it, and edited in JSON. Taken for text, building it again turned an address into a text box.
    /// </summary>
    internal static readonly (string Kind, BormFieldType Type)[] Kept =
    [
        ("hidden", BormFieldType.Hidden), ("object", BormFieldType.Object), ("array", BormFieldType.Array),
        ("custom", BormFieldType.Custom)
    ];

    internal static readonly HashSet<string> Textual = ["text", "textarea", "markdown", "email", "password", "url", "tel", "search"];
    internal static readonly HashSet<string> Numeric = ["number", "integer", "range"];
    internal static readonly HashSet<string> Choice = ["select", "radio", "combobox", "multiselect"];

    public string Kind { get; set; } = "text";
    public string Name { get; set; } = "";
    public string? Label { get; set; }
    public string? Help { get; set; }
    public string? Placeholder { get; set; }

    /// <summary>What a static field says.</summary>
    public string? Text { get; set; }

    public bool Required { get; set; }
    public bool ReadOnly { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Pattern { get; set; }
    public string? Mask { get; set; }

    /// <summary>One option per line: <c>value = Label</c>, or just the value when it is its own label.</summary>
    public string? Options { get; set; }

    public string? VisibleWhen { get; set; }
    public string? RequiredWhen { get; set; }
    public string? Computed { get; set; }
    public string? Group { get; set; }
    public int? ColumnSpan { get; set; }

    /// <summary>The definition this field was read from, whose settings the designer has no place for are kept.</summary>
    internal BormFieldDefinition? Original { get; set; }

    /// <summary>Whether the original holds something the designer cannot show - code it keeps as it is.</summary>
    internal bool KeepsCode { get; set; }

    /// <summary>Whether this is a kind the designer keeps rather than offers (see <see cref="Kept"/>).</summary>
    internal bool IsKept => !Kinds.Any(k => k.Kind == Kind);

    // A kind of field newer than both tables is kept as it is too: its type is the one it was read with.
    internal BormFieldType Type => Kinds.Concat(Kept).FirstOrDefault(k => k.Kind == Kind) is { Kind: not null } known
        ? known.Type
        : Original?.Type ?? BormFieldType.Text;

    /// <summary>Builds the field this describes. Throws <see cref="FormatException"/> for an expression or mask that does not read.</summary>
    internal BormFieldDefinition Build()
    {
        var type = Type;
        var field = Original is { } original
            // A copy, so building again from the same original starts from what it was, not from the last build.
            ? new BormDefinition { Fields = [original] }.Clone().Fields[0]
            : new BormFieldDefinition(Name, type);

        field.Name = Name;
        field.Type = type;
        field.Label = string.IsNullOrWhiteSpace(Label) ? BormFieldBuilder.Humanize(Name) : Label;
        field.HelpText = Blank(Help);
        field.Placeholder = Blank(Placeholder);
        field.Group = Blank(Group);
        field.ColumnSpan = ColumnSpan is > 1 ? ColumnSpan : null;
        field.ReadOnly = ReadOnly;

        var builder = new BormFieldBuilder(field);
        if (IsKept)
        {
            // Only what the panel shows is written; its lengths, limits, options and rules stay as they were - and
            // so does how it is required, unless that was changed here.
            if (Required != field.Required)
            {
                field.Required = false;
                field.RemoveValidators("required");
                if (Required) builder.Required();
            }
        }
        else
        {
            field.Required = false;
            field.MinLength = field.MaxLength = null;
            field.Min = field.Max = null;
            field.Pattern = null;
            field.Mask = null;
            field.MaskStoresRaw = false;
            foreach (var key in (string[])["required", "minLength", "maxLength", "range", "pattern", BormMessageKeys.Mask])
                field.RemoveValidators(key);
            if (Kind == "switch") field.Attributes["switch"] = true;
            else field.Attributes.Remove("switch");

            if (type == BormFieldType.Static)
            {
                // A static field's label is its heading and its help is its text, as the builder's Static() makes them.
                field.HelpText = Blank(Text);
                field.ShowLabel = !string.IsNullOrWhiteSpace(Label);
                return field;
            }

            if (Required) builder.Required();
        }

        if (Textual.Contains(Kind))
        {
            if (MinLength is > 0 and var min) builder.MinLength(min);
            if (MaxLength is > 0 and var max) builder.MaxLength(max);
            if (Blank(Pattern) is { } pattern) builder.Pattern(pattern);
            if (Blank(Mask) is { } mask)
            {
                try
                {
                    builder.Mask(mask);
                }
                catch (ArgumentException ex)
                {
                    throw new FormatException(ex.Message, ex);
                }
            }
        }
        if (Numeric.Contains(Kind) && (Min is not null || Max is not null)) builder.Range(Min, Max);

        if (Choice.Contains(Kind) || Kind == "tags")
        {
            field.Options.Clear();
            builder.Options([.. ParseOptions(Options)]);
        }

        if (Blank(VisibleWhen) is { } visible) builder.VisibleWhen(visible);
        else if (field.VisibleWhen is BormExpressionCondition) field.VisibleWhen = null;
        if (Blank(RequiredWhen) is { } required) builder.RequiredWhen(required);
        else if (field.RequiredWhen is BormExpressionCondition) field.RequiredWhen = null;
        if (Blank(Computed) is { } computed) builder.Computed(computed);
        else if (field.Computed?.Target is BormExpression) field.Computed = null;
        return field;
    }

    /// <summary>Reads a field of an existing schema into the designer, keeping what it cannot show.</summary>
    internal static BormDesignerField From(BormFieldDefinition field)
    {
        var kind = field.Type == BormFieldType.Checkbox && field.Attributes.TryGetValue("switch", out var s) && s is true
            ? "switch"
            : Kinds.Concat(Kept).FirstOrDefault(k => k.Type == field.Type).Kind ?? field.Type.ToString().ToLowerInvariant();

        var designer = new BormDesignerField
        {
            Kind = kind,
            Name = field.Name,
            Label = field.Label,
            Help = field.Type == BormFieldType.Static ? null : field.HelpText,
            Placeholder = field.Placeholder,
            Text = field.Type == BormFieldType.Static ? field.HelpText : null,
            Required = field.Required,
            ReadOnly = field.ReadOnly,
            MinLength = field.MinLength,
            MaxLength = field.MaxLength,
            Min = field.Min,
            Max = field.Max,
            Pattern = field.Pattern,
            Mask = field.Mask,
            Options = field.Options.Count == 0 ? null : string.Join('\n', field.Options.Select(o => o.Value == o.Label ? o.Value : $"{o.Value} = {o.Label}")),
            VisibleWhen = (field.VisibleWhen as BormExpressionCondition)?.Expression.Text,
            RequiredWhen = (field.RequiredWhen as BormExpressionCondition)?.Expression.Text,
            Computed = (field.Computed?.Target as BormExpression)?.Text,
            Group = field.Group,
            ColumnSpan = field.ColumnSpan,
            Original = field
        };
        designer.KeepsCode = field.OptionsProvider is not null
                             || field.VisibleWhen is not null and not BormExpressionCondition
                             || field.RequiredWhen is not null and not BormExpressionCondition
                             || field.Computed is not null && field.Computed.Target is not BormExpression
                             || field.Type is BormFieldType.Object or BormFieldType.Array or BormFieldType.Custom
                             || field.Validators.Any(v => v.Key is null);
        return designer;
    }

    /// <summary>A copy for another field like this one, with a name of its own.</summary>
    internal BormDesignerField Copy(string name)
    {
        var copy = (BormDesignerField)MemberwiseClone();
        copy.Name = name;
        if (!string.IsNullOrWhiteSpace(Label)) copy.Label = Label + " (2)";
        return copy;
    }

    internal static IEnumerable<BormSelectOption> ParseOptions(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in text.Split('\n'))
        {
            var entry = line.Trim();
            if (entry.Length == 0) continue;
            var equals = entry.IndexOf('=');
            var value = (equals > 0 ? entry[..equals] : entry).Trim();
            var label = equals > 0 ? entry[(equals + 1)..].Trim() : value;
            if (value.Length > 0 && seen.Add(value)) yield return new BormSelectOption(value, label.Length > 0 ? label : value);
        }
    }

    /// <summary>A name like <paramref name="stem"/> no field in <paramref name="taken"/> has yet.</summary>
    internal static string FreeName(string stem, IEnumerable<string> taken)
    {
        // The copy of email1 is email2, not email11.
        stem = stem.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        if (stem.Length == 0) stem = "field";
        var names = new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase);
        for (var i = 1; ; i++)
        {
            var name = stem + i.ToString(CultureInfo.InvariantCulture);
            if (!names.Contains(name)) return name;
        }
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
