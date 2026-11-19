namespace Bit.Borm;

/// <summary>What a <see cref="BormChangeHandler"/> is given when it runs.</summary>
/// <remarks>
/// Paths are read and written relative to <see cref="ScopePath"/> — the object that owns the field —
/// exactly as a computed value's dependencies and a condition's paths are, so a handler written on a
/// repeater's item template means <em>that row</em> without knowing which row it landed on.
/// </remarks>
public sealed class BormChangeContext(BormState state, string fieldPath, string scopePath)
{
    /// <summary>The form the change happened in.</summary>
    public BormState State { get; } = state;

    /// <summary>The absolute path of the field that changed, e.g. <c>Rows[2].Country</c>.</summary>
    public string FieldPath { get; } = fieldPath;

    /// <summary>The path of the object that owns the field — <c>Rows[2]</c>, or empty at the root.</summary>
    public string ScopePath { get; } = scopePath;

    /// <summary>The field's new value.</summary>
    public object? Value => State.GetValue(FieldPath);

    /// <summary>Reads a sibling by its name relative to <see cref="ScopePath"/>.</summary>
    public object? Sibling(string relativePath) => State.GetValue(Resolve(relativePath));

    /// <summary>Reads any path from the root of the form.</summary>
    public object? Absolute(string absolutePath) => State.GetValue(absolutePath);

    /// <summary>
    /// Writes a sibling, relative to <see cref="ScopePath"/>. The write does not mark the field
    /// touched, because the user has not been there — a field the form filled in on their behalf must
    /// not open covered in errors.
    /// </summary>
    public void SetSibling(string relativePath, object? value) => State.SetValueQuietly(Resolve(relativePath), value);

    /// <summary>Writes any path from the root of the form. See <see cref="SetSibling"/> on touched state.</summary>
    public void Set(string absolutePath, object? value) => State.SetValueQuietly(absolutePath, value);

    /// <summary>Empties a sibling — the "clear the city when the country changes" case, spelled out.</summary>
    public void ClearSibling(string relativePath) => SetSibling(relativePath, null);

    private string Resolve(string relativePath)
        => ScopePath.Length == 0 ? relativePath : BormPath.Combine(ScopePath, relativePath);
}
