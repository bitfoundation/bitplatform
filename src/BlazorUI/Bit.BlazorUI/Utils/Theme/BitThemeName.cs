namespace Bit.BlazorUI;

/// <summary>
/// Type-safe wrapper for the <c>bit-theme</c> attribute name. Use the static factories
/// (<see cref="Light"/>, <see cref="Dark"/>, <see cref="Fluent"/>, etc.) for the well-known presets;
/// call <see cref="Custom(string)"/> for app-specific theme names that match your CSS. The Material and
/// Cupertino design systems ship with the <c>Bit.BlazorUI.Extras</c> package; their names are the
/// factories on <c>BitExtraThemeName</c> there.
/// </summary>
/// <remarks>
/// <para>
/// Existing callers can keep using the <c>string</c> constants on <see cref="BitThemePresets"/>;
/// this struct is additive and provides an implicit conversion to <see cref="string"/> so it slots
/// straight into <see cref="BitThemeManager.SetThemeAsync"/> without ceremony.
/// </para>
/// <para>
/// The wrapper normalizes the value (trim + lower-case invariant) so <c>Custom("Fluent-Light")</c>
/// and <c>BitThemeName.FluentLight</c> compare equal. That mirrors the JS side, which writes the
/// value verbatim onto <c>document.documentElement</c> and lets CSS selectors (<c>:root[bit-theme="…"]</c>)
/// match in a case-sensitive fashion against the canonical lowercase form.
/// </para>
/// </remarks>
public readonly struct BitThemeName : IEquatable<BitThemeName>
{
    private readonly string? _value;

    private BitThemeName(string value)
    {
        _value = value;
    }

    /// <summary>The normalized theme name (trimmed, lower-case invariant).</summary>
    public string Value => _value ?? BitThemePresets.Light;

    /// <summary>Light preset (<c>"light"</c>).</summary>
    public static BitThemeName Light { get; } = new(BitThemePresets.Light);

    /// <summary>Dark preset (<c>"dark"</c>).</summary>
    public static BitThemeName Dark { get; } = new(BitThemePresets.Dark);

    /// <summary>Fluent base preset (<c>"fluent"</c>).</summary>
    public static BitThemeName Fluent { get; } = new(BitThemePresets.Fluent);

    /// <summary>Fluent light preset (<c>"fluent-light"</c>).</summary>
    public static BitThemeName FluentLight { get; } = new(BitThemePresets.FluentLight);

    /// <summary>Fluent dark preset (<c>"fluent-dark"</c>).</summary>
    public static BitThemeName FluentDark { get; } = new(BitThemePresets.FluentDark);

    /// <summary>Special pseudo-preset that follows OS <c>prefers-color-scheme</c> (<c>"system"</c>).</summary>
    public static BitThemeName System { get; } = new(BitThemePresets.System);

    /// <summary>
    /// Wraps a custom theme name. Whitespace is trimmed and the result is lower-cased. The normalized
    /// value must be a safe <c>[a-z0-9-]</c> token of at most 64 characters so it matches the SSR
    /// theme-token parsing rules (see <c>BitThemeSsr.NormalizeThemeToken</c>) and keeps runtime and
    /// first-paint validation consistent for persisted custom themes.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// When <paramref name="name"/> is null, empty, whitespace, longer than 64 characters, or contains
    /// characters outside <c>[a-z0-9-]</c> after normalization.
    /// </exception>
    public static BitThemeName Custom(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var token = NormalizeToken(name, out var error) ?? throw new ArgumentException(error, nameof(name));

        return new BitThemeName(token);
    }

    /// <summary>
    /// Normalizes (trim + lower-case invariant) and validates a theme token, returning <see langword="null"/>
    /// with a reason in <paramref name="error"/> when the value is blank, longer than 64 characters, or contains
    /// characters outside <c>[a-z0-9-]</c>. Shared with <c>BitThemeSsr.NormalizeThemeToken</c> so runtime and
    /// first-paint validation stay identical; each caller maps the failure onto its own contract
    /// (<see cref="Custom(string)"/> throws, the SSR helper returns <see langword="null"/>).
    /// </summary>
    internal static string? NormalizeToken(string? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Theme name must not be empty or whitespace.";
            return null;
        }

        var token = value.Trim().ToLowerInvariant();

        if (token.Length > 64)
        {
            error = "Theme name must be at most 64 characters.";
            return null;
        }

        foreach (var ch in token)
        {
            var allowed = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-';
            if (allowed is false)
            {
                error = "Theme name may only contain lower-case letters, digits, and hyphens.";
                return null;
            }
        }

        error = null;
        return token;
    }

    /// <summary>Implicitly converts to the underlying string so existing string-typed APIs accept the wrapper unchanged.</summary>
    public static implicit operator string(BitThemeName name) => name.Value;

    public override string ToString() => Value;

    public bool Equals(BitThemeName other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is BitThemeName other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(BitThemeName left, BitThemeName right) => left.Equals(right);
    public static bool operator !=(BitThemeName left, BitThemeName right) => !left.Equals(right);
}
