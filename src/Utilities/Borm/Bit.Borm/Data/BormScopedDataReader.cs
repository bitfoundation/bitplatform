namespace Bit.Borm;

/// <summary>
/// A view over form data that resolves a path relative to an owning object before falling back to the
/// root. This is what lets a condition written on an array item's template — <c>VisibleWhen("Kind", …)</c>
/// — mean *this row's* Kind, exactly as a computed value's dependencies already do, while a condition
/// naming a top-level field keeps working from anywhere in the form.
/// </summary>
public sealed class BormScopedDataReader : IBormDataReader
{
    private readonly IBormDataReader _inner;
    private readonly string _scope;

    private BormScopedDataReader(IBormDataReader inner, string scope)
    {
        _inner = inner;
        _scope = scope;
    }

    /// <summary>
    /// Wraps <paramref name="data"/> so paths resolve against <paramref name="scope"/> first. Returns
    /// <paramref name="data"/> unchanged for the root scope, so the common case allocates nothing.
    /// </summary>
    public static IBormDataReader For(IBormDataReader data, string scope)
        => string.IsNullOrEmpty(scope) ? data : new BormScopedDataReader(Unwrap(data), scope);

    /// <summary>Wraps <paramref name="data"/> scoped to the container that owns <paramref name="fieldPath"/>.</summary>
    public static IBormDataReader ForOwnerOf(IBormDataReader data, string fieldPath)
        => For(data, BormPath.Parent(fieldPath));

    /// <summary>The root of the underlying store — a scope never changes what "the whole form" means.</summary>
    public object? Root => _inner.Root;

    /// <summary>The object the scope points at, for rules that want the owning row rather than the model.</summary>
    public object? Scope => _inner.GetValue(_scope);

    /// <summary>The path this reader is scoped to.</summary>
    public string ScopePath => _scope;

    public object? GetValue(string path) => TryGetValue(path, out var value) ? value : null;

    /// <summary>
    /// Resolves against the scope first, then the root.
    /// </summary>
    /// <remarks>
    /// The test is whether the scoped path <em>exists</em>, not whether it holds something. A row's own
    /// field wins even when it is empty — which is the whole point of a condition such as
    /// <c>VisibleWhen("Email", IsEmpty)</c>. Falling back on "the scoped read came out null" let a
    /// root-level field of the same name answer for the row, so a blank row silently reported its
    /// neighbour's value. The fallback still catches the case it was written for: a path the row does
    /// not have at all, which is how an absolute reference from inside a repeater keeps working.
    /// </remarks>
    public bool TryGetValue(string path, out object? value)
    {
        // An empty path means "the whole store", which no scope changes.
        if (!string.IsNullOrEmpty(path)
            && _inner.TryGetValue(BormPath.Combine(_scope, path), out value))
            return true;

        return _inner.TryGetValue(path, out value);
    }

    /// <summary>
    /// Where a read of <paramref name="path"/> through <paramref name="data"/> actually lands: the
    /// unscoped store and the absolute path that answers it — the scoped one when the owning object has
    /// it, the root one otherwise. An expression walking every row of a list (<c>{Lines[].Total}</c>)
    /// needs the absolute path of the list it found to read each row from.
    /// </summary>
    internal static (IBormDataReader Store, string Path) Resolve(IBormDataReader data, string path)
    {
        if (data is not BormScopedDataReader scoped || string.IsNullOrEmpty(path)) return (data, path);

        var combined = BormPath.Combine(scoped._scope, path);
        return scoped._inner.TryGetValue(combined, out _) ? (scoped._inner, combined) : (scoped._inner, path);
    }

    /// <summary>Peels off an existing scope so scopes never nest into an unreadable chain.</summary>
    private static IBormDataReader Unwrap(IBormDataReader data)
        => data is BormScopedDataReader scoped ? scoped._inner : data;
}
