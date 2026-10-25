namespace Bit.BlazorUI;

/// <summary>
/// Resolved color list and lookup helpers. Built from the calendar's <c>EventColorOptions</c>
/// parameter (or the built-in <see cref="BitFullCalendarColorOption.Defaults"/> palette when
/// none was supplied). Events reference a color through <see cref="BitFullCalendarColorOption.Id"/>.
/// </summary>
public sealed class BitFullCalendarColorScheme
{
    /// <summary>
    /// Preferred id for events with no explicit color, used only when the configured palette actually
    /// contains it. When a custom palette omits this id, the resolver falls back to the first
    /// configured swatch instead (see <see cref="_fallbackId"/>).
    /// </summary>
    public const string FallbackColorId = "blue";

    /// <summary>Inline style emitted on color-bearing elements (bullets, swatches, chips, blocks).</summary>
    public const string ColorVariableName = "--bit-bfc-evt-color";

    private readonly Dictionary<string, BitFullCalendarColorOption> _byId;

    /// <summary>
    /// The id that blank (null/empty/whitespace) color ids resolve to. Prefers
    /// <see cref="FallbackColorId"/> when the configured palette contains it, otherwise the first
    /// configured swatch, so default-colored events always map to a real entry in the current scheme
    /// rather than assuming "blue" exists.
    /// </summary>
    private readonly string _fallbackId;

    public BitFullCalendarColorScheme(IReadOnlyList<BitFullCalendarColorOption>? options)
    {
        var list = options is { Count: > 0 } ? options : BitFullCalendarColorOption.Defaults;
        // Build Options and the _byId lookup from the SAME canonicalized sequence: trim ids, skip
        // blanks, and keep only the first occurrence of each id (case-insensitive). Otherwise Options
        // (consumed by the UI / filters / GetSortOrder) could expose blank or duplicate entries that
        // the id resolver silently ignores, so what the user sees would drift from what Find resolves.
        _byId = new Dictionary<string, BitFullCalendarColorOption>(StringComparer.OrdinalIgnoreCase);
        var canonical = new List<BitFullCalendarColorOption>(list.Count);
        foreach (var o in list)
        {
            var id = o.Id?.Trim();
            if (string.IsNullOrEmpty(id) || _byId.ContainsKey(id))
                continue;
            // Store a normalized copy (trimmed id) in BOTH collections so Options never exposes an
            // untrimmed id that Find would otherwise silently resolve through its trimmed key.
            var normalized = string.Equals(o.Id, id, StringComparison.Ordinal)
                ? o
                : new BitFullCalendarColorOption { Id = id, Title = o.Title, Value = o.Value };
            _byId[id] = normalized;
            canonical.Add(normalized);
        }
        // Wrap in a read-only view so consumers can't mutate Options after construction and
        // desynchronize it from the _byId lookup it was built alongside.
        Options = canonical.AsReadOnly();

        // Resolve the blank-id fallback against the CURRENT scheme: prefer "blue" when present,
        // otherwise the first configured swatch. Only assume the "blue" literal when nothing was
        // configured at all (Options is empty), so blank ids never point at a non-existent entry.
        _fallbackId = _byId.ContainsKey(FallbackColorId)
            ? FallbackColorId
            : (Options.Count > 0 ? Options[0].Id : FallbackColorId);
    }

    /// <summary>Configured colors in display order.</summary>
    public IReadOnlyList<BitFullCalendarColorOption> Options { get; }

    /// <summary>
    /// Maps a blank (null/empty/whitespace) color id to the scheme's resolved fallback swatch
    /// (<see cref="_fallbackId"/>) and trims the rest, so blank ids resolve to the same swatch as the
    /// default-colored events everywhere (lookup, label, css value, sort order) instead of drifting
    /// between methods.
    /// </summary>
    private string NormalizeId(string? colorId)
    {
        if (string.IsNullOrWhiteSpace(colorId))
            return _fallbackId;

        var trimmed = colorId.Trim();
        // An event that never set a color carries the default id, which a custom palette may not have: it is drawn in
        // that palette's fallback swatch like a blank one, not in the default event color meant for unknown ids.
        return string.Equals(trimmed, FallbackColorId, StringComparison.OrdinalIgnoreCase) && _byId.ContainsKey(trimmed) is false
            ? _fallbackId
            : trimmed;
    }

    /// <summary>Looks up a color option by id (case-insensitive). Returns null when unknown.</summary>
    public BitFullCalendarColorOption? Find(string? colorId)
    {
        return _byId.TryGetValue(NormalizeId(colorId), out var o) ? o : null;
    }

    /// <summary>
    /// The id an event's color resolves to: the matching option's own id (so a default-colored event and one set to the
    /// palette's fallback swatch are one color everywhere - grouping, filtering, editing), else the trimmed value.
    /// </summary>
    public string GetCanonicalId(string? colorId) => Find(colorId)?.Id ?? colorId?.Trim() ?? string.Empty;

    /// <summary>Display label for dropdowns, filters, agenda headers, and event details.</summary>
    public string GetLabel(string? colorId)
    {
        var opt = Find(colorId);
        if (opt is not null && !string.IsNullOrWhiteSpace(opt.Title))
            return opt.Title;
        // Trim the raw fallback so whitespace-padded/unknown ids resolve to a cleaned label,
        // consistent with the trimming applied everywhere else in the resolver.
        return colorId?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// The value an event is drawn in when its color is neither a configured option nor a CSS color: the calendar's
    /// default event color, which the public <c>--bit-FullCalendar-event-color</c> variable sets.
    /// </summary>
    public const string DefaultCssValue = "var(--bit-bfc-event-default)";

    /// <summary>
    /// CSS color value for the supplied id: the matching option's <see cref="BitFullCalendarColorOption.Value"/>, else the
    /// id itself when it is a CSS color (a hex such as <c>"#e91e63"</c> or a functional notation such as
    /// <c>"rgb(...)"</c>, <c>"oklch(...)"</c> or <c>"var(...)"</c>), else <see cref="DefaultCssValue"/>.
    /// </summary>
    public string GetCssValue(string? colorId)
    {
        var opt = Find(colorId);
        if (opt is not null && !string.IsNullOrWhiteSpace(opt.Value))
            return opt.Value;

        var raw = colorId?.Trim();
        return raw is not null && IsCssColor(raw) ? raw : DefaultCssValue;
    }

    /// <summary>
    /// True when <paramref name="value"/> is a hex color or a CSS color function, so an event can carry a color of its
    /// own without a matching option. Anything able to break out of the inline style it is written into is refused.
    /// </summary>
    internal static bool IsCssColor(string value)
    {
        if (value.Length < 4)
            return false;

        if (value[0] == '#')
            return value.Length is 4 or 5 or 7 or 9 && value.AsSpan(1).ContainsAnyExcept(_hexDigits) is false;

        // Only the characters a color function is written with - no quotes, semicolons, braces, backslashes or
        // comment stars - so the value can never leave the custom property it is written into.
        if (value.AsSpan().ContainsAnyExcept(_colorFunctionChars))
            return false;

        var paren = value.IndexOf('(');
        if (paren <= 0 || value[^1] != ')')
            return false;

        if (value[..paren].ToLowerInvariant() is not ("rgb" or "rgba" or "hsl" or "hsla" or "hwb" or "lab" or "lch"
                                                       or "oklab" or "oklch" or "color" or "color-mix" or "light-dark" or "var"))
            return false;

        // One call and nothing after it: "rgb(0 0 0) url(...)" would otherwise turn a background into an image request.
        var depth = 0;
        for (var i = paren; i < value.Length; i++)
        {
            if (value[i] == '(') depth++;
            else if (value[i] == ')' && --depth == 0 && i != value.Length - 1) return false;
            if (depth < 0) return false;
        }
        if (depth != 0)
            return false;

        // Nor anything inside it - a var() fallback included - that loads or paints something other than a color: the
        // value feeds background shorthands, where an image or a gradient would render.
        foreach (var refused in _refusedCssTerms)
        {
            if (value.Contains(refused, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static readonly string[] _refusedCssTerms = ["url(", "image", "attr(", "expression", "gradient", "element(", "cross-fade", "paint("];
    private static readonly System.Buffers.SearchValues<char> _hexDigits = System.Buffers.SearchValues.Create("0123456789abcdefABCDEF");
    private static readonly System.Buffers.SearchValues<char> _colorFunctionChars =
        System.Buffers.SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 #(),.%-+/_");

    /// <summary>
    /// Inline style string that publishes the resolved color value as the
    /// <see cref="ColorVariableName"/> CSS custom property. Combine with the matching CSS classes
    /// (e.g. <c>bit-bfc-color</c>, <c>bit-bfc-bg</c>, <c>bit-bfc-bullet</c>) to render the chip surface.
    /// </summary>
    public string GetColorStyle(string? colorId) =>
        $"{ColorVariableName}:{GetCssValue(colorId)};";

    /// <summary>
    /// Options shown in the add/edit dialog. If the event references an id that is not in
    /// <see cref="Options"/> (for example a color removed at runtime) the missing entry is
    /// appended so the value remains selectable.
    /// </summary>
    public IReadOnlyList<BitFullCalendarColorOption> GetEditorOptions(string? editingColorId)
    {
        if (string.IsNullOrWhiteSpace(editingColorId) || Find(editingColorId) is not null)
            return Options;

        var extra = new List<BitFullCalendarColorOption>(Options.Count + 1);
        extra.AddRange(Options);
        extra.Add(new BitFullCalendarColorOption
        {
            Id = editingColorId.Trim(),
            Title = editingColorId.Trim(),
            Value = GetCssValue(editingColorId)
        });
        return extra;
    }

    /// <summary>Sort key for agenda grouping - configured order first, then unknown ids (sorted by name at the call site).</summary>
    public int GetSortOrder(string? colorId)
    {
        var trimmed = NormalizeId(colorId);
        for (var i = 0; i < Options.Count; i++)
        {
            if (string.Equals(Options[i].Id?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        // Deterministic: all unknown ids share the same key and are ordered lexically by a secondary sort.
        return int.MaxValue;
    }
}
