namespace Bit.BlazorUI;

/// <summary>
/// Describes one selectable event color in the calendar UI (picker, filters, agenda headers,
/// event badges, bullets, swatches). The list and order are controlled by the calendar component's
/// <c>EventColorOptions</c> parameter; events reference a color through its <see cref="Id"/>.
/// </summary>
public sealed class BitFullCalendarColorOption
{
    /// <summary>
    /// Stable identifier of the color (matched against <see cref="BitFullCalendarEvent.Color"/>).
    /// Treated case-insensitively. Use a short, slug-style value such as <c>"blue"</c> or <c>"skyblue"</c>.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display label shown in pickers, filters, agenda headers, and event details. This is the full
    /// human-readable color name and is used as-is (no localization/transformation is applied).
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// CSS color value used for swatches, bullets, badge accents, and chip surfaces - any value
    /// accepted in CSS such as a hex (<c>"#3b82f6"</c>), <c>rgb()</c>, <c>hsl()</c>, or named color
    /// (<c>"skyblue"</c>). The calendar derives badge background, border, and text contrast tints
    /// from this single value at runtime.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Built-in palette used when <c>EventColorOptions</c> is <c>null</c> or empty. The IDs
    /// (<c>"blue"</c>, <c>"green"</c>, ...) match the values previously emitted by
    /// <c>BitFullCalendarEventColor</c>, so events created against the defaults need no migration.
    /// </summary>
    public static IReadOnlyList<BitFullCalendarColorOption> Defaults { get; } =
        new BitFullCalendarColorOption[]
        {
            new() { Id = "blue",   Title = "Blue",   Value = "#3b82f6" },
            new() { Id = "green",  Title = "Green",  Value = "#22c55e" },
            new() { Id = "red",    Title = "Red",    Value = "#ef4444" },
            new() { Id = "yellow", Title = "Yellow", Value = "#eab308" },
            new() { Id = "purple", Title = "Purple", Value = "#a855f7" },
            new() { Id = "orange", Title = "Orange", Value = "#f97316" },
        }.AsReadOnly();
}
