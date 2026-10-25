namespace Bit.Borm;

/// <summary>
/// What a form's state needs to know about its schema on every change, worked out once: which fields
/// take part in each of the sweeps a write runs (clearing hidden values, recomputing, reloading
/// cascading choices, revalidating dependents, normalising, judging visibility), and which definition a
/// path resolves to.
/// </summary>
/// <remarks>
/// <para>
/// Every write used to walk every field of the form, once per sweep, materialising a list of every
/// live path each time — so a keystroke into one box of a two-thousand-field form paid for two
/// thousand fields several times over, and a computed total over two hundred invoice lines walked the
/// whole invoice once per line just to be seeded. A sweep only ever acts on the fields that declare
/// what it handles, so it now visits those alone: each is kept as a template that expands into the
/// paths of the rows that exist right now.
/// </para>
/// <para>
/// The index describes the schema as it was when it was built. A form that changes its definition
/// under a live state says so through <see cref="BormState.NotifyDefinitionChanged"/>, which builds it
/// again — the same contract the sweeps' "does this schema use it at all" flags always had.
/// </para>
/// </remarks>
internal sealed class BormFieldIndex
{
    private readonly BormDefinition _definition;
    private readonly Dictionary<string, BormFieldDefinition> _byTemplatePath = new(StringComparer.OrdinalIgnoreCase);

    public BormFieldIndex(BormDefinition definition)
    {
        _definition = definition;

        var all = new List<BormFieldTemplate>();
        foreach (var field in definition.Fields)
            Collect(field, [field.Name], all);

        ClearOnHide = all.Where(t => t.Field.ClearOnHide && t.Field.VisibleWhen is not null).ToArray();
        Computed = all.Where(t => t.Field.Computed is not null).ToArray();
        CascadingOptions = all.Where(t => t.Field.OptionsProvider is not null && t.Field.OptionsDependencies.Count > 0).ToArray();
        Revalidating = all.Where(t => t.Field.RevalidateOn.Count > 0).ToArray();
        Normalizing = all.Where(t => t.Field.Normalize is not null).ToArray();
        Conditional = all.Where(t => t.Field.VisibleWhen is not null).ToArray();
        HasChangeHandlers = all.Any(t => t.Field.OnChanged is not null);
    }

    /// <summary>Fields cleared when a condition hides them.</summary>
    public BormFieldTemplate[] ClearOnHide { get; }

    /// <summary>Fields whose value is derived from others.</summary>
    public BormFieldTemplate[] Computed { get; }

    /// <summary>Fields whose loaded choices depend on another field.</summary>
    public BormFieldTemplate[] CascadingOptions { get; }

    /// <summary>Fields revalidated when another field changes.</summary>
    public BormFieldTemplate[] Revalidating { get; }

    /// <summary>Fields with a normalizer.</summary>
    public BormFieldTemplate[] Normalizing { get; }

    /// <summary>Fields with a visibility condition, containers before what they contain.</summary>
    public BormFieldTemplate[] Conditional { get; }

    public bool HasChangeHandlers { get; }

    /// <summary>
    /// The definition <paramref name="path"/> resolves to, as <see cref="BormDefinition.FindByPath"/>
    /// answers it, remembered per template: every row of a list shares a definition, so
    /// <c>Lines[3].Qty</c> and <c>Lines[40].Qty</c> are one lookup.
    /// </summary>
    /// <remarks>
    /// Only hits are remembered. A path that names nothing yet — a <c>BormField</c> placed before the
    /// field it shows was added to a schema built up at run time — is asked again next time, so it
    /// starts resolving the moment the field exists; the hot paths only ever ask about fields that do.
    /// </remarks>
    public BormFieldDefinition? Find(string path)
    {
        var key = TemplateKey(path);
        if (_byTemplatePath.TryGetValue(key, out var field)) return field;

        // A malformed path names no field; FindByPath would say so by throwing from the parser.
        field = BormPath.IsValid(path) ? _definition.FindByPath(path) : null;
        if (field is not null) _byTemplatePath[key] = field;
        return field;
    }

    /// <summary>A path with every row index taken out — <c>Lines[3].Qty</c> becomes <c>Lines[].Qty</c>.</summary>
    internal static string TemplateKey(string path)
    {
        if (path.IndexOf('[') < 0) return path;

        var sb = new System.Text.StringBuilder(path.Length);
        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            sb.Append(c);
            if (c != '[') continue;
            while (i + 1 < path.Length && char.IsAsciiDigit(path[i + 1])) i++;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Mirrors the walk the state's full enumeration makes, so a template expands into exactly the
    /// paths that walk would have produced: an object's children beneath it, a list's rows beneath the
    /// list, and an object row's children directly beneath the row (the row's template itself is not
    /// a field of its own).
    /// </summary>
    private static void Collect(BormFieldDefinition field, List<string?> pieces, List<BormFieldTemplate> into)
    {
        into.Add(new BormFieldTemplate(field, [.. pieces]));

        if (field.Type == BormFieldType.Object)
        {
            foreach (var child in field.Children)
                Collect(child, [.. pieces, child.Name], into);
        }
        else if (field.Type == BormFieldType.Array && field.ItemTemplate is { } template)
        {
            if (template.Type == BormFieldType.Object)
            {
                foreach (var child in template.Children)
                    Collect(child, [.. pieces, null, child.Name], into);
            }
            else
            {
                Collect(template, [.. pieces, null], into);
            }
        }
    }
}

/// <summary>
/// One field of the schema and where it sits: names, with <c>null</c> standing for "every row of the
/// list so far", so <c>["Lines", null, "Total"]</c> is every line's total.
/// </summary>
internal sealed class BormFieldTemplate(BormFieldDefinition field, string?[] pieces)
{
    public BormFieldDefinition Field { get; } = field;

    private readonly string?[] _pieces = pieces;

    /// <summary>
    /// The live paths of this field, in row order, given how many rows each list holds right now.
    /// A field outside any list has exactly one path.
    /// </summary>
    public void Expand(Func<string, int> count, List<(BormFieldDefinition Field, string Path)> into)
        => Expand(count, 0, string.Empty, into);

    private void Expand(Func<string, int> count, int at, string prefix, List<(BormFieldDefinition, string)> into)
    {
        for (var i = at; i < _pieces.Length; i++)
        {
            if (_pieces[i] is { } name)
            {
                // The first piece is a top-level field's own name, which may itself contain dots
                // (`x => x.Address.City`); joined as written, it is exactly the path the walk produced.
                prefix = prefix.Length == 0 ? name : BormPath.Combine(prefix, name);
                continue;
            }

            var rows = count(prefix);
            for (var row = 0; row < rows; row++)
                Expand(count, i + 1, BormPath.Combine(prefix, row), into);
            return;
        }
        into.Add((Field, prefix));
    }
}
