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
        Options = list;
        _byId = new Dictionary<string, BitFullCalendarColorOption>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in list)
        {
            var id = o.Id?.Trim();
            if (!string.IsNullOrEmpty(id) && !_byId.ContainsKey(id))
                _byId[id] = o;
        }
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
        return first?.Value ?? "#3b82f6";
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

    /// <summary>Sort key for agenda grouping — configured order first, then unknown ids by name.</summary>
    public int GetSortOrder(string? colorId)
    {
        if (string.IsNullOrWhiteSpace(colorId))
            return int.MaxValue;
        var trimmed = colorId.Trim();
        for (var i = 0; i < Options.Count; i++)
        {
            if (string.Equals(Options[i].Id, trimmed, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return 1000 + StringComparer.OrdinalIgnoreCase.GetHashCode(trimmed);
    }
}
