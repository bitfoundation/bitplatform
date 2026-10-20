namespace Bit.Borm;

/// <summary>
/// Runs when a field's value changes — the schema's own "and when this changes, do that". Clearing the
/// city when the country changes, resetting a seat count when the plan does, stamping a timestamp: the
/// things every form ends up needing and that otherwise have to be wired up outside the schema by
/// subscribing to <see cref="BormState.FieldChanged"/> and working out which path fired.
/// </summary>
/// <remarks>
/// This is the missing third of a set. <see cref="BormComputedValue"/> derives a value *from*
/// others, <see cref="BormFieldDefinition.OptionsDependencies"/> reloads choices, and
/// <see cref="BormFieldDefinition.ClearOnHide"/> empties a hidden branch — but "when A changes,
/// write B" fits none of them, because B is still the user's to edit afterwards. TanStack Form calls
/// these listeners; Formly calls them hooks.
/// </remarks>
public delegate void BormChangeHandler(BormChangeContext context);

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
