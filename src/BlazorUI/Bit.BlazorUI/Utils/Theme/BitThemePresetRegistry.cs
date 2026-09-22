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
/// own, its <c>Add...Services</c> call) is itself what loads it. An app that never mentions the
/// package in C#, and writes the <c>bit-theme</c> token as a bare string instead, leaves its presets
/// unregistered: the first-paint color then falls back to the scheme's light / dark surface (see
/// <see cref="BitThemeHead"/>) and everything else, being CSS, is unaffected. Naming the preset
/// through <see cref="BitThemePresets"/> or <see cref="BitThemeName"/> rather than as a string avoids
/// that and is what the docs show.
/// </para>
/// <para>
/// <b>An app's word is final, whatever the order.</b> <see cref="Register(BitThemePreset)"/> is the
/// app's verb and replaces: registering a name again is how an app re-skins a packaged preset it has
/// overridden in its own CSS. <see cref="TryRegister(BitThemePreset)"/> is a package's verb and only
/// fills a gap: it leaves alone a name that is already registered, and a name the app has taken out
/// with <see cref="Remove(string?)"/>. A package registers whenever its assembly happens to load -
/// often after <c>Program.cs</c> has run - so without that split the app's re-skin or removal would
/// be silently undone by the first thing that touched the package.
/// </para>
/// <para>
/// Registration is thread-safe. Writes are rare (startup) and swap in a new immutable snapshot, so
/// the reads a host page makes on every request are lock-free and have nothing to copy or sort.
/// </para>
/// </remarks>
public static class BitThemePresetRegistry
{
    // The surfaces the core stylesheet paints for its own light / dark palettes. Named, because
    // BitThemeHead's last-resort fallback needs them even when an app has removed the presets that
    // carry them from the table.
    internal const string CoreLightBackgroundPrimary = "#FFFFFF";
    internal const string CoreLightBackgroundSecondary = "#F5F5F5";
    internal const string CoreDarkBackgroundPrimary = "#0F1318";
    internal const string CoreDarkBackgroundSecondary = "#1B2025";

    /// <summary>
    /// What a read sees: the presets by token, the same presets ordered by name, and the tokens the
    /// app has removed. Never mutated once published and replaced whole on every write, so a reader
    /// never observes a half-applied change and never has to sort.
    /// </summary>
    private sealed class Snapshot(Dictionary<string, BitThemePreset> byName, HashSet<string> removed)
    {
        public Dictionary<string, BitThemePreset> ByName { get; } = byName;

        /// <summary>Tokens <see cref="Remove(string?)"/> was called for, which <see cref="TryRegister(BitThemePreset)"/> must not bring back.</summary>
        public HashSet<string> Removed { get; } = removed;

        public BitThemePreset[] Ordered { get; } = [.. byName.Values.OrderBy(preset => preset.Name, StringComparer.Ordinal)];
    }

    private static readonly object _writeLock = new();
    private static volatile Snapshot _snapshot = new(new(StringComparer.Ordinal), new(StringComparer.Ordinal));

    static BitThemePresetRegistry()
    {
        // The presets the core stylesheet itself implements. light / dark are the same two palettes
        // under their family-less names, and fluent is the family's base alias: colors.fluent-light
        // selects all three (:root[bit-theme="light"], [bit-theme="fluent"], [bit-theme="fluent-light"])
        // and colors.fluent-dark the other two. Every name a stylesheet paints has to be in this
        // table under its own key, or a host page's first paint falls back to a color the stylesheet
        // that follows it never applies.
        Register(
        [
            new() { Name = BitThemePresets.Light, BackgroundPrimary = CoreLightBackgroundPrimary, BackgroundSecondary = CoreLightBackgroundSecondary },
            new() { Name = BitThemePresets.Dark, BackgroundPrimary = CoreDarkBackgroundPrimary, BackgroundSecondary = CoreDarkBackgroundSecondary },
            new() { Name = BitThemePresets.Fluent, BackgroundPrimary = CoreLightBackgroundPrimary, BackgroundSecondary = CoreLightBackgroundSecondary },
            new() { Name = BitThemePresets.FluentLight, BackgroundPrimary = CoreLightBackgroundPrimary, BackgroundSecondary = CoreLightBackgroundSecondary },
            new() { Name = BitThemePresets.FluentDark, BackgroundPrimary = CoreDarkBackgroundPrimary, BackgroundSecondary = CoreDarkBackgroundSecondary },
        ]);
    }

    /// <summary>
    /// Adds a preset, replacing any already registered under the same name - the verb for an APP,
    /// whose declaration wins over a package's whichever of the two runs first. The name is
    /// normalized and validated exactly as <see cref="BitThemeName.Custom(string)"/> validates one,
    /// so a preset cannot enter the table under a token the first-paint parser would reject.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// When the name is null, empty, whitespace, longer than 64 characters, or contains characters
    /// outside <c>[a-z0-9-]</c> after normalization.
    /// </exception>
    public static void Register(BitThemePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        Write([preset], replace: true, nameof(preset));
    }

    /// <summary>
    /// Adds several presets, as <see cref="Register(BitThemePreset)"/> does each. All of the names are
    /// validated before any is applied, so a bad token leaves the table as it was.
    /// </summary>
    public static void Register(IEnumerable<BitThemePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        Write(presets, replace: true, nameof(presets));
    }

    /// <summary>
    /// Adds a preset unless the app has already spoken for its name - the verb for a PACKAGE declaring
    /// the presets its stylesheet ships. Leaves the table untouched, and answers
    /// <see langword="false"/>, when a preset is already registered under the name or the name has
    /// been taken out with <see cref="Remove(string?)"/>. The name is validated as
    /// <see cref="Register(BitThemePreset)"/> validates it.
    /// </summary>
    /// <remarks>
    /// A package registers from a module initializer, which runs whenever something first touches
    /// its assembly - typically after the app's startup code. Replace semantics there would undo an
    /// app's re-skin (<see cref="Register(BitThemePreset)"/>) or removal of a packaged preset, at a
    /// moment the app cannot see.
    /// </remarks>
    /// <exception cref="ArgumentException">As <see cref="Register(BitThemePreset)"/>.</exception>
    public static bool TryRegister(BitThemePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return Write([preset], replace: false, nameof(preset)) == 1;
    }

    /// <summary>
    /// Adds several presets as <see cref="TryRegister(BitThemePreset)"/> does each, and reports how
    /// many were added.
    /// </summary>
    public static int TryRegister(IEnumerable<BitThemePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        return Write(presets, replace: false, nameof(presets));
    }

    /// <summary>
    /// Removes the preset registered under a name, and reports whether there was one. The name is
    /// normalized as <see cref="Register(BitThemePreset)"/> normalizes it, so a preset is removed by
    /// whatever spelling registered it; a name that is not a valid token was never in the table and
    /// is answered with <see langword="false"/> rather than an exception.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The counterpart to registration, for an app that offers a subset of the packaged presets and
    /// does not want the rest in its first-paint table (<see cref="BitThemeSurfaces"/>, and so the
    /// lookup <see cref="BitThemeHead"/> emits) - and for a test that registers a preset of its own
    /// and has to leave the process-global table as it found it. The table is all it affects: a
    /// theme picker is not driven by it (<c>BitThemeSwitcher</c> offers what its own parameters
    /// say), and the stylesheet that implements the preset is whatever the app links.
    /// </para>
    /// <para>
    /// Removal is remembered: a package's <see cref="TryRegister(BitThemePreset)"/> does not bring the
    /// name back, even when the package only loads after this call - which is also why a name that
    /// is not in the table YET is still recorded. <see cref="Register(BitThemePreset)"/> does bring
    /// it back.
    /// </para>
    /// </remarks>
    public static bool Remove(string? name)
    {
        var token = BitThemeName.NormalizeToken(name, out _);
        if (token is null) return false;

        lock (_writeLock)
        {
            var current = _snapshot;
            var existed = current.ByName.ContainsKey(token);

            // Nothing to take out and already on record: no new snapshot for a no-op.
            if (existed is false && current.Removed.Contains(token)) return false;

            var byName = new Dictionary<string, BitThemePreset>(current.ByName, StringComparer.Ordinal);
            byName.Remove(token);

            _snapshot = new Snapshot(byName, new HashSet<string>(current.Removed, StringComparer.Ordinal) { token });

            return existed;
        }
    }

    /// <summary>The preset registered under a name, or <see langword="null"/> when none is.</summary>
    public static BitThemePreset? Find(string? name)
    {
        var token = BitThemeName.NormalizeToken(name, out _);

        return token is not null && _snapshot.ByName.TryGetValue(token, out var preset) ? preset : null;
    }

    /// <summary>Whether a preset is registered under a name.</summary>
    public static bool Contains(string? name) => Find(name) is not null;

    /// <summary>
    /// Every registered preset, ordered by name so a caller that renders them (a picker, a lookup
    /// table in a script) produces the same output on every render.
    /// </summary>
    public static IReadOnlyList<BitThemePreset> All => Array.AsReadOnly(_snapshot.Ordered);

    /// <summary>
    /// Enumerates the presets that carry a value for one surface, ordered by name, for the
    /// <see cref="BitThemeSurfaces"/> views. Internal: an app reads the surfaces through those two maps.
    /// </summary>
    internal static IEnumerable<KeyValuePair<string, string>> SurfacesOf(Func<BitThemePreset, string?> surface)
    {
        // One snapshot for the whole enumeration, already ordered: nothing to copy, nothing to sort.
        foreach (var preset in _snapshot.Ordered)
        {
            if (surface(preset) is { } color)
            {
                yield return new KeyValuePair<string, string>(preset.Name, color);
            }
        }
    }

    /// <summary>How many presets carry a value for one surface.</summary>
    internal static int SurfaceCountOf(Func<BitThemePreset, string?> surface)
    {
        var count = 0;
        foreach (var preset in _snapshot.Ordered)
        {
            if (surface(preset) is not null) count++;
        }

        return count;
    }

    /// <summary>
    /// The color the preset stored under exactly this key paints for a surface, or
    /// <see langword="null"/>. Ordinal and un-normalized, unlike <see cref="Find(string?)"/>: this
    /// backs a dictionary view, whose lookups have to agree with the keys it enumerates.
    /// </summary>
    internal static string? SurfaceOf(string key, Func<BitThemePreset, string?> surface)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _snapshot.ByName.TryGetValue(key, out var preset) ? surface(preset) : null;
    }

    /// <summary>
    /// Applies a batch under one lock and one snapshot swap, and reports how many entries it wrote.
    /// Every name is validated before anything is applied, so a bad token in the middle of a batch
    /// leaves the table as it was.
    /// </summary>
    private static int Write(IEnumerable<BitThemePreset> presets, bool replace, string paramName)
    {
        var normalized = new List<BitThemePreset>();
        foreach (var preset in presets)
        {
            if (preset is null) throw new ArgumentException("A preset in the collection is null.", paramName);

            var token = BitThemeName.NormalizeToken(preset.Name, out var error) ?? throw new ArgumentException(error, paramName);

            normalized.Add(token == preset.Name ? preset : new BitThemePreset
            {
                Name = token,
                BackgroundPrimary = preset.BackgroundPrimary,
                BackgroundSecondary = preset.BackgroundSecondary,
            });
        }

        lock (_writeLock)
        {
            var current = _snapshot;
            var byName = new Dictionary<string, BitThemePreset>(current.ByName, StringComparer.Ordinal);
            var removed = new HashSet<string>(current.Removed, StringComparer.Ordinal);
            var written = 0;

            foreach (var preset in normalized)
            {
                if (replace)
                {
                    byName[preset.Name] = preset;
                    removed.Remove(preset.Name);
                    written++;
                }
                else if (removed.Contains(preset.Name) is false && byName.TryAdd(preset.Name, preset))
                {
                    written++;
                }
            }

            if (written > 0) _snapshot = new Snapshot(byName, removed);

            return written;
        }
    }
}
