namespace Bit.Borm;

/// <summary>Options controlling reflection-based schema generation.</summary>
public sealed class BormSchemaGeneratorOptions
{
    /// <summary>
    /// Maximum nesting depth for object graphs (guards against cycles). At this depth an object is
    /// generated without its children, and a list of objects — which cannot render a row without them —
    /// is left out; a list of plain values is generated at any depth.
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>When true, properties marked <c>[ScaffoldColumn(false)]</c> are skipped.</summary>
    public bool HonorScaffoldColumn { get; set; } = true;

    /// <summary>
    /// How to treat a property with no setter. Computed properties are usually worth showing but never
    /// worth letting the user edit, so the default renders them read-only rather than dropping them.
    /// </summary>
    public BormReadOnlyPropertyHandling ReadOnlyProperties { get; set; } = BormReadOnlyPropertyHandling.RenderReadOnly;

    /// <summary>Property names to leave out of the generated schema (case-insensitive).</summary>
    public ISet<string> IgnoredProperties { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Called for every generated field, after DataAnnotations have been applied — the hook for
    /// project-wide conventions such as "every field named *Email gets autocomplete".
    /// </summary>
    public Action<BormFieldDefinition>? ConfigureField { get; set; }
}

/// <summary>What <see cref="BormSchemaGenerator"/> does with get-only properties.</summary>
public enum BormReadOnlyPropertyHandling
{
    /// <summary>Include them, marked read-only.</summary>
    RenderReadOnly,

    /// <summary>Leave them out of the schema entirely.</summary>
    Skip
}
