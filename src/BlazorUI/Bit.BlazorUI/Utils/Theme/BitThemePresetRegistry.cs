using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// The table of theme presets this process knows, and the extension point every package and app adds
/// to. The core package registers the Fluent family its own stylesheet carries; Bit.BlazorUI.Extras
/// registers Fluent 2, Material and Cupertino when it loads; an app registers its own with
/// <see cref="Register(BitThemePreset)"/>. <see cref="BitThemeSurfaces"/> is a live view over this,
/// so whatever registers is immediately part of the one table <see cref="BitThemeHead"/> reads.
/// </summary>
/// <remarks>
/// <para>
/// This exists so that a package which ships a stylesheet can contribute a preset to the core
/// theming APIs instead of standing up a parallel set of its own that an app would then have to know
/// about. A design system that lives in another assembly is a first-class preset here, not a
/// second-class one.
/// </para>
/// <para>
/// <b>When a package's presets register.</b> A package registers from a module initializer, which the
/// runtime runs before the first access to anything in that assembly - so naming one of its presets
/// in C# (<c>BitThemePresets.MaterialDark</c>, <c>BitThemeName.MaterialDark</c>, a component of its
/// own) is itself what loads it. An app that never mentions the package in C#, and writes the
/// <c>bit-theme</c> token as a bare string instead, leaves its presets unregistered: the first-paint
/// color then falls back to the scheme's light / dark surface (see <see cref="BitThemeHead"/>) and
/// everything else, being CSS, is unaffected. Naming the preset through
/// <see cref="BitThemePresets"/> or <see cref="BitThemeName"/> rather than as a string avoids that
/// and is what the docs show.
/// </para>
/// <para>
/// Registration is thread-safe and idempotent by name: registering a name again replaces it, which is
/// how an app re-skins a packaged preset it has overridden in its own CSS.
/// </para>
/// </remarks>
public static class BitThemePresetRegistry
{
    private static readonly ConcurrentDictionary<string, BitThemePreset> _presets = new(StringComparer.Ordinal);

    static BitThemePresetRegistry()
    {
        // The presets the core stylesheet itself implements. light / dark are the same two palettes
        // under their family-less names: colors.fluent-light and colors.fluent-dark each select both
        // (:root[bit-theme="light"], [bit-theme="fluent-light"]).
        Register(new BitThemePreset { Name = BitThemePresets.Light, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" });
        Register(new BitThemePreset { Name = BitThemePresets.Dark, BackgroundPrimary = "#0F1318", BackgroundSecondary = "#1B2025" });
        Register(new BitThemePreset { Name = BitThemePresets.FluentLight, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" });
        Register(new BitThemePreset { Name = BitThemePresets.FluentDark, BackgroundPrimary = "#0F1318", BackgroundSecondary = "#1B2025" });
    }

    /// <summary>
    /// Adds a preset, replacing any already registered under the same name. The name is normalized and
    /// validated exactly as <see cref="BitThemeName.Custom(string)"/> validates one, so a preset
    /// cannot enter the table under a token the first-paint parser would reject.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// When the name is null, empty, whitespace, longer than 64 characters, or contains characters
    /// outside <c>[a-z0-9-]</c> after normalization.
    /// </exception>
    public static void Register(BitThemePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var token = BitThemeName.NormalizeToken(preset.Name, out var error) ?? throw new ArgumentException(error, nameof(preset));

        _presets[token] = token == preset.Name ? preset : new BitThemePreset
        {
            Name = token,
            BackgroundPrimary = preset.BackgroundPrimary,
            BackgroundSecondary = preset.BackgroundSecondary,
        };
    }

    /// <summary>Adds several presets, as <see cref="Register(BitThemePreset)"/> does each.</summary>
    public static void Register(IEnumerable<BitThemePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        foreach (var preset in presets)
        {
            Register(preset);
        }
    }

    /// <summary>The preset registered under a name, or <see langword="null"/> when none is.</summary>
    public static BitThemePreset? Find(string? name)
    {
        var token = BitThemeName.NormalizeToken(name, out _);

        return token is not null && _presets.TryGetValue(token, out var preset) ? preset : null;
    }

    /// <summary>Whether a preset is registered under a name.</summary>
    public static bool Contains(string? name) => Find(name) is not null;

    /// <summary>
    /// Every registered preset, ordered by name so a caller that renders them (a picker, a lookup
    /// table in a script) produces the same output on every render.
    /// </summary>
    public static IReadOnlyList<BitThemePreset> All =>
        [.. _presets.Values.OrderBy(preset => preset.Name, StringComparer.Ordinal)];

    /// <summary>
    /// Enumerates the presets that carry a value for one surface, for the <see cref="BitThemeSurfaces"/>
    /// views. Internal: an app reads the surfaces through those two maps.
    /// </summary>
    internal static IEnumerable<KeyValuePair<string, string>> SurfacesOf(Func<BitThemePreset, string?> surface)
    {
        foreach (var preset in _presets.Values.OrderBy(preset => preset.Name, StringComparer.Ordinal))
        {
            if (surface(preset) is { } color)
            {
                yield return new KeyValuePair<string, string>(preset.Name, color);
            }
        }
    }

    /// <summary>The color one preset paints for a surface, or <see langword="null"/>.</summary>
    internal static string? SurfaceOf(string? name, Func<BitThemePreset, string?> surface)
    {
        var preset = Find(name);

        return preset is null ? null : surface(preset);
    }
}
