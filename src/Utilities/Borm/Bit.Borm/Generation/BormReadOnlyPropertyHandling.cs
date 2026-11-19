namespace Bit.Borm;

/// <summary>What <see cref="BormSchemaGenerator"/> does with get-only properties.</summary>
public enum BormReadOnlyPropertyHandling
{
    /// <summary>Include them, marked read-only.</summary>
    RenderReadOnly,

    /// <summary>Leave them out of the schema entirely.</summary>
    Skip
}
