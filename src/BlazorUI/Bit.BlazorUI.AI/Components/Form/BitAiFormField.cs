namespace Bit.BlazorUI;

/// <summary>
/// One field of a model-generated form.
/// </summary>
/// <param name="Name">The key the value is submitted under.</param>
/// <param name="Label">The visible label. Falls back to a humanised name.</param>
public sealed record BitAiFormField(string Name, string? Label = null)
{
    /// <summary>The control to render.</summary>
    public BitAiFieldType Type { get; init; } = BitAiFieldType.Text;

    /// <summary>Help text under the control.</summary>
    public string? Description { get; init; }

    /// <summary>Placeholder text inside the control.</summary>
    public string? Placeholder { get; init; }

    /// <summary>
    /// The initial value, which a model may prefill. For an <see cref="BitAiFieldType.Array"/> field
    /// this is the JSON text of the initial array.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>Whether the field must be filled in.</summary>
    public bool Required { get; init; }

    /// <summary>Whether the field is shown but cannot be edited, as for a schema <c>const</c> or <c>readOnly</c>.</summary>
    public bool ReadOnly { get; init; }

    /// <summary>Choices for a select field, as the values that are submitted.</summary>
    public IReadOnlyList<string>? Options { get; init; }

    /// <summary>Display labels for <see cref="Options"/>, in the same order, when they differ from the values.</summary>
    public IReadOnlyList<string>? OptionLabels { get; init; }

    /// <summary>The smallest accepted value, for numbers and dates.</summary>
    public double? Minimum { get; init; }

    /// <summary>The largest accepted value, for numbers and dates.</summary>
    public double? Maximum { get; init; }

    /// <summary>A value the number must be strictly greater than.</summary>
    public double? ExclusiveMinimum { get; init; }

    /// <summary>A value the number must be strictly less than.</summary>
    public double? ExclusiveMaximum { get; init; }

    /// <summary>A step the number must be a multiple of.</summary>
    public double? MultipleOf { get; init; }

    /// <summary>Whether a numeric field accepts whole numbers only.</summary>
    public bool Integer { get; init; }

    /// <summary>The fewest characters a text value may have.</summary>
    public int? MinLength { get; init; }

    /// <summary>The most characters a text value may have.</summary>
    public int? MaxLength { get; init; }

    /// <summary>The fewest entries an array field may have.</summary>
    public int? MinItems { get; init; }

    /// <summary>The most entries an array field may have.</summary>
    public int? MaxItems { get; init; }

    /// <summary>A regular expression the value must match.</summary>
    public string? Pattern { get; init; }

    /// <summary>The nested fields of an <see cref="BitAiFieldType.Object"/> field.</summary>
    public IReadOnlyList<BitAiFormField>? Fields { get; init; }

    /// <summary>The template for each entry of an <see cref="BitAiFieldType.Array"/> field.</summary>
    public BitAiFormField? Items { get; init; }

    /// <summary>
    /// The JSON Schema primitive the value is submitted as: <c>string</c>, <c>integer</c>,
    /// <c>number</c>, <c>boolean</c>, <c>object</c> or <c>array</c>. Decides how
    /// <see cref="BitAiFormDefinition.ToJson"/> encodes the typed text; <see langword="null"/> means string.
    /// </summary>
    public string? JsonType { get; init; }

    /// <summary>
    /// The label shown for the field: <see cref="Label"/>, or else the <see cref="Name"/> written as a label
    /// ("firstName" reads "First Name", "first_name" reads "First name").
    /// </summary>
    public string DisplayLabel => Label ?? Humanize(Name);

    /// <summary>
    /// Turns a field name into something a person would write on a label: "firstName" becomes "First Name" and
    /// "first_name" becomes "First name".
    /// </summary>
    /// <remarks>
    /// Only the first letter is capitalised; every other letter keeps its case. An underscore or a hyphen becomes a space,
    /// and an upper-case letter that does not follow another one starts a new word.
    /// </remarks>
    /// <param name="name">The field name.</param>
    /// <returns>The label.</returns>
    internal static string Humanize(string name)
    {
        if (name.Length == 0) return name;

        var builder = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is '_' or '-') { builder.Append(' '); continue; }
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1])) builder.Append(' ');
            builder.Append(i == 0 ? char.ToUpperInvariant(c) : c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Whether two fields describe the same control: same type, choices, constraints and prefill,
    /// recursively. Used to decide whether a re-created definition is a new schema or the same one
    /// streamed again, without being fooled by the fresh list instances a rebuild produces.
    /// </summary>
    internal static bool SameShape(BitAiFormField? a, BitAiFormField? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        if (a.Name != b.Name || a.Type != b.Type || a.Value != b.Value || a.Required != b.Required
            || a.ReadOnly != b.ReadOnly || a.Integer != b.Integer || a.JsonType != b.JsonType
            || a.Minimum != b.Minimum || a.Maximum != b.Maximum || a.Pattern != b.Pattern)
        {
            return false;
        }

        if (!SameList(a.Options, b.Options)) return false;
        if (!SameShape(a.Items, b.Items)) return false;

        var af = a.Fields ?? [];
        var bf = b.Fields ?? [];
        if (af.Count != bf.Count) return false;
        for (var i = 0; i < af.Count; i++)
        {
            if (!SameShape(af[i], bf[i])) return false;
        }

        return true;
    }

    private static bool SameList(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return a is null && b is null;
        return a.SequenceEqual(b, StringComparer.Ordinal);
    }
}
