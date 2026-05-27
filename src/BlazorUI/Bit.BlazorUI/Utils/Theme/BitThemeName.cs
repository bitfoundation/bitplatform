namespace Bit.BlazorUI;

/// <summary>
/// Type-safe wrapper for the <c>bit-theme</c> attribute name. Use the static factories
/// (<see cref="Light"/>, <see cref="Dark"/>, <see cref="Fluent"/>, etc.) for the well-known presets;
/// call <see cref="Custom(string)"/> for app-specific theme names that match your CSS.
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

    /// <summary>Wraps a custom theme name. Whitespace is trimmed and the result is lower-cased.</summary>
    /// <exception cref="ArgumentException">When <paramref name="name"/> is null, empty, or whitespace.</exception>
    public static BitThemeName Custom(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new BitThemeName(name.Trim().ToLowerInvariant());
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
