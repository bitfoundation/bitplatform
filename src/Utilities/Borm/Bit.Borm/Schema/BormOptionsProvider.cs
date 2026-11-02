namespace Bit.Borm;

/// <summary>
/// Loads the choices for a select/radio/multi-select field on demand. This is what makes cascading
/// selects possible — "cities" reading the currently selected "country" — and lets options come from a
/// service instead of being baked into the schema.
/// </summary>
public delegate ValueTask<IReadOnlyList<BormSelectOption>> BormOptionsProvider(BormOptionsContext context);

/// <summary>What an <see cref="BormOptionsProvider"/> is given when it runs.</summary>
public sealed class BormOptionsContext(
    BormFieldDefinition field,
    string fieldPath,
    IBormDataReader data,
    IServiceProvider? services,
    CancellationToken cancellationToken = default)
{
    /// <summary>The field whose options are being loaded.</summary>
    public BormFieldDefinition Field { get; } = field;

    /// <summary>The absolute path of the field within the form data.</summary>
    public string FieldPath { get; } = fieldPath;

    /// <summary>
    /// Read access to the whole form, for options that depend on other values. Paths resolve against
    /// the object that owns the field before the root, so inside a repeater <c>"Country"</c> is that
    /// row's country — the same thing the field's <c>dependsOn</c> means.
    /// </summary>
    public IBormDataReader Data { get; } = data;

    /// <summary>Service provider for resolving whatever the provider needs (HTTP clients, repositories…).</summary>
    public IServiceProvider? Services { get; } = services;

    /// <summary>Cancelled when a newer load supersedes this one, or the form is disposed.</summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// For a field that searches its options (<see cref="BormFieldDefinition.SearchMinLength"/>): what
    /// the user has typed so far, trimmed. Null when the field opens with a value and the provider is
    /// asked only for the option behind <see cref="CurrentValue"/>, so the box can show its label.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>The value the field holds while its options are loaded.</summary>
    public object? CurrentValue { get; init; }

    /// <summary>Reads a dependency value, e.g. the selected country for a cities lookup.</summary>
    public object? Value(string path) => Data.GetValue(path);
}
