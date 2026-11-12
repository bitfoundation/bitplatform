namespace Bit.BlazorUI;

/// <summary>
/// What the source generator writes into every params type of the packages that is declared partial: the snapshot and
/// the merge <see cref="BitParamsScope"/> takes of it, written out property by property instead of read off the type by
/// reflection. A WebAssembly app's interpreter pays dearly for reflection the first time it runs it (the properties
/// loaded, an invoker made for every getter and setter), and the params objects an app sets up as it starts are taken
/// apart while its first page is being built.
/// </summary>
/// <remarks>
/// Both answer null for an object of a type derived from the one they were generated for, whose properties they do not
/// know: it is taken apart by reflection instead, exactly as before.
/// </remarks>
internal interface IBitGeneratedParams
{
    /// <summary>
    /// The snapshot of the object, the same that <see cref="BitParamsScope"/> takes of it by reflection, or null when
    /// the object is of a derived type.
    /// </summary>
    object? TakeSnapshot();

    /// <summary>
    /// A new object of the same type, in which every parameter this one leaves unset is taken from
    /// <paramref name="basis"/> when that is of the very same type, or null when the object is of a derived type.
    /// </summary>
    IBitComponentParams? MergeOver(IBitComponentParams? basis);
}

/// <summary>
/// The parts of a snapshot the generated code of the params types builds its own out of, shared with the reflection
/// that takes apart everything else.
/// </summary>
internal static class BitParamsSnapshot
{
    /// <summary>
    /// How deep a value is taken apart for its snapshot; anything deeper is compared as it is.
    /// </summary>
    public const int MaxDepth = BitParamsScope.MaxSnapshotDepth;

    /// <summary>
    /// The snapshot of an object taken apart property by property.
    /// </summary>
    public static object Object(Type type, object?[] values) => new BitParamsScope.ObjectSnapshot(type, values);

    /// <summary>
    /// The snapshot of any value, taken by reflection where it has to be taken apart.
    /// </summary>
    public static object? Of(object? value, int depth) => BitParamsScope.TakeSnapshot(value, depth);

    /// <summary>
    /// What a merged copy carries for a parameter of a type a dictionary of attributes can be assigned to: the
    /// attributes of both, those of <paramref name="value"/> winning, when it holds a dictionary; otherwise the value,
    /// or the basis' where it is unset.
    /// </summary>
    public static object? MergeAttributes(object? value, object? basisValue) => BitParamsScope.MergeAttributes(value, basisValue);
}
