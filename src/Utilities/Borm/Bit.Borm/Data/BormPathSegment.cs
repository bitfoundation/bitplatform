namespace Bit.Borm;

/// <summary>One segment of a form path: either a named property or an array index.</summary>
public readonly struct BormPathSegment
{
    private BormPathSegment(string? name, int index)
    {
        Name = name;
        Index = index;
    }

    /// <summary>Property name, or null when this is an index segment.</summary>
    public string? Name { get; }

    /// <summary>Array index, or -1 when this is a property segment.</summary>
    public int Index { get; }

    public bool IsIndex => Name is null;

    public static BormPathSegment Property(string name) => new(name, -1);
    public static BormPathSegment At(int index) => new(null, index);

    public override string ToString() => IsIndex ? $"[{Index}]" : Name!;
}
