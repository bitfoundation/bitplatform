namespace Bit.BlazorUI;

/// <summary>
/// Resolved color list and lookup helpers. Built from the calendar's <c>EventColorOptions</c>
/// parameter (or the built-in <see cref="BitFullCalendarColorOption.Defaults"/> palette when
/// none was supplied). Events reference a color through <see cref="BitFullCalendarColorOption.Id"/>.
/// </summary>
public sealed class BitFullCalendarColorScheme
{
    /// <summary>Id used when an event's <see cref="BitFullCalendarEvent.Color"/> is null/empty.</summary>
    public const string FallbackColorId = "blue";

    /// <summary>Inline style emitted on color-bearing elements (bullets, swatches, chips, blocks).</summary>
    public const string ColorVariableName = "--bit-bfc-evt-color";

    private readonly Dictionary<string, BitFullCalendarColorOption> _byId;

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
        Options = canonical;
    }

    /// <summary>Configured colors in display order.</summary>
    public IReadOnlyList<BitFullCalendarColorOption> Options { get; }

    /// <summary>Looks up a color option by id (case-insensitive). Returns null when unknown.</summary>
    public BitFullCalendarColorOption? Find(string? colorId)
    {
        if (string.IsNullOrWhiteSpace(colorId))
            return null;
        return _byId.TryGetValue(colorId.Trim(), out var o) ? o : null;
    }

    /// <summary>Display label for dropdowns, filters, agenda headers, and event details.</summary>
    public string GetLabel(string? colorId)
    {
        var opt = Find(colorId);
        if (opt is not null && !string.IsNullOrWhiteSpace(opt.Title))
            return opt.Title;
        return colorId ?? string.Empty;
    }

    /// <summary>CSS color value for the supplied id (falls back to the first configured color).</summary>
    public string GetCssValue(string? colorId)
    {
        var opt = Find(colorId);
        if (opt is not null && !string.IsNullOrWhiteSpace(opt.Value))
            return opt.Value;
        var first = Options.Count > 0 ? Options[0] : null;
        return !string.IsNullOrWhiteSpace(first?.Value) ? first!.Value : "#3b82f6";
    }

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
        if (string.IsNullOrWhiteSpace(editingColorId) || _byId.ContainsKey(editingColorId.Trim()))
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
        if (string.IsNullOrWhiteSpace(colorId))
            return int.MaxValue;
        var trimmed = colorId.Trim();
        for (var i = 0; i < Options.Count; i++)
        {
            if (string.Equals(Options[i].Id?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        // Deterministic: all unknown ids share the same key and are ordered lexically by a secondary sort.
        return int.MaxValue;
    }
}
