using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A date of a <see cref="BitBhCalendarGrid"/>: a <c>&lt;td&gt;</c> (<c>gridcell</c>) named by the full date
/// ("Sunday, November 8, 2026"), with <c>aria-selected</c>, <c>aria-current="date"</c> on today and
/// <c>aria-disabled</c> on dates that can't be selected. The focused date is the grid's one tab stop; dates outside
/// <c>Min</c> / <c>Max</c> can't be focused. Dates of the neighbouring months that fill the first and last weeks are
/// empty (see <see cref="BitBhCalendar.ShowOutsideDays"/>).
///
/// <para>Shows the day number unless given content; <see cref="Template"/> receives the <see cref="BitBhCalendarDay"/>.</para>
///
/// <para><b>Styling hooks:</b> <c>data-today</c>, <c>data-outside-month</c>, <c>data-unavailable</c>, <c>data-disabled</c>,
/// <c>data-selected</c>, <c>data-focused</c>, <c>data-highlighted</c>, <c>data-selection-start</c>, <c>data-selection-end</c>,
/// <c>data-range-middle</c>, <c>data-invalid</c>, plus <c>data-hover</c> / <c>data-active</c> / <c>data-focus-visible</c>
/// from the runtime and <c>data-date</c> (ISO).</para>
/// </summary>
public class BitBhCalendarCell : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhCalendarContext? Context { get; set; }

    /// <summary>The date to render: the context of the grid's content (<c>Day="context"</c>).</summary>
    [Parameter, EditorRequired]
    public BitBhCalendarDay Day { get; set; } = default!;

    /// <summary>Replaces the day number.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content rendered from the date's state: <c>&lt;Template Context="day"&gt;…&lt;/Template&gt;</c>.</summary>
    [Parameter]
    public RenderFragment<BitBhCalendarDay>? Template { get; set; }

    protected override string DefaultTag => "td";

    private BitBhCalendar? Owner => Context?.Owner;

    // A date of the neighbouring month is only a filler: nothing to focus or select.
    private bool IsInteractive => Owner is not null && Day is { IsOutsideMonth: false };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        if (IsInteractive) builder.AddAttribute(10, "id", Owner!.CellId(Day.Date));
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsInteractive && Owner is { } owner)
        {
            var date = Day.Date;
            builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => owner.SelectAsync(date)));
            builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => owner.HandleKeyDownAsync(date, e)));
            // While a range is being picked, Escape cancels it - and only it, not the popover around the calendar.
            builder.AddEventStopPropagationAttribute(32, "onkeydown", owner.Anchor is not null);
            builder.AddAttribute(33, "onfocus", On<FocusEventArgs>("onfocus", () => owner.HandleFocusAsync(date)));
            if (owner.Anchor is not null)
                builder.AddAttribute(34, "onpointerenter", On<PointerEventArgs>("onpointerenter", () => owner.HandlePointerEnter(date)));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (!Day.IsOutsideMonth || (Owner?.ShowOutsideDays ?? true))
        {
            if (Template is not null || ChildContent is not null) builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, Day));
            else builder.AddContent(51, Day.Text);
        }
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Owner is not { } owner || Day is null) return attrs;
        var day = Day;

        SetDataValue(attrs, "date", BitBhCalendar.Iso(day.Date));
        if (day.IsOutsideMonth)
        {
            // Shown only for its looks: assistive technology reads the month's own dates.
            if (owner.ShowOutsideDays) attrs["aria-hidden"] = "true";
            SetDataFlag(attrs, "outside-month", true);
            SetDataFlag(attrs, "disabled", true);
            SetDataFlag(attrs, "today", day.IsToday);
            return attrs;
        }

        attrs["aria-label"] = day.Label;
        if (!day.IsDisabled) attrs["tabindex"] = day.IsFocused ? "0" : "-1";
        if (day.IsSelected) attrs["aria-selected"] = "true";
        else if (owner.EffectiveMode != BitBhCalendarSelectionMode.Single && !day.IsDisabled && !day.IsUnavailable) attrs["aria-selected"] = "false";
        if (day.IsDisabled || day.IsUnavailable) attrs["aria-disabled"] = "true";
        if (day.IsToday) attrs["aria-current"] = "date";
        if (!day.IsDisabled)
        {
            attrs[BitBhKeyboard.KeysAttribute] = owner.Anchor is not null
                ? "ArrowUp ArrowDown ArrowLeft ArrowRight PageUp PageDown Home End Enter Space Escape"
                : "ArrowUp ArrowDown ArrowLeft ArrowRight PageUp PageDown Home End Enter Space";
            attrs["data-bit-bh-interactive"] = "";
            // Opened in a date picker, the popover moves the focus to the focused date.
            if (owner.InPicker && day.IsFocused) attrs["data-bit-bh-autofocus"] = "";
        }

        SetDataFlag(attrs, "today", day.IsToday);
        SetDataFlag(attrs, "unavailable", day.IsUnavailable);
        SetDataFlag(attrs, "disabled", day.IsDisabled);
        SetDataFlag(attrs, "selected", day.IsSelected);
        SetDataFlag(attrs, "focused", day.IsFocused);
        SetDataFlag(attrs, "highlighted", day.IsHighlighted);
        SetDataFlag(attrs, "selection-start", day.IsSelectionStart && owner.EffectiveMode == BitBhCalendarSelectionMode.Range);
        SetDataFlag(attrs, "selection-end", day.IsSelectionEnd && owner.EffectiveMode == BitBhCalendarSelectionMode.Range);
        SetDataFlag(attrs, "range-middle", day.IsRangeMiddle);
        SetDataFlag(attrs, "invalid", day.IsInvalid);
        SetDataValue(attrs, "weekday", ((int)day.DayOfWeek).ToString(CultureInfo.InvariantCulture));
        return attrs;
    }
}
