using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// One month of a <see cref="BitBhCalendar"/>: a <c>&lt;table role="grid"&gt;</c> with a header row of week days
/// (abbreviated, with the full name as <c>abbr</c> and accessible name) and a row per week. Its accessible name is the
/// month ("November 2026") — the <see cref="BitBhCalendarHeading"/> when one month is shown.
///
/// <para>Each date renders a <see cref="BitBhCalendarCell"/>. Give the grid content to render the cells yourself (to
/// style them, or give them a <c>Template</c>); its context is the <see cref="BitBhCalendarDay"/>:</para>
/// <code>
/// &lt;BitBhCalendarGrid class="calendar-grid"&gt;
///     &lt;BitBhCalendarCell Day="context" class="calendar-cell" /&gt;
/// &lt;/BitBhCalendarGrid&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>; style the header cells with
/// a selector on the grid (<c>.calendar-grid th</c>).</para>
/// </summary>
public class BitBhCalendarGrid : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhCalendarContext? Context { get; set; }

    /// <summary>Which visible month the grid shows: 0 for the first, 1 for the next … (see <see cref="BitBhCalendar.VisibleMonths"/>).</summary>
    [Parameter]
    public int Offset { get; set; }

    /// <summary>How the week-day headers name the days. Default: <see cref="BitBhCalendarWeekdayFormat.Narrow"/>.</summary>
    [Parameter]
    public BitBhCalendarWeekdayFormat WeekdayFormat { get; set; }

    /// <summary>The content of each week-day header (its accessible name stays the full name of the day).</summary>
    [Parameter]
    public RenderFragment<BitBhCalendarWeekday>? WeekdayTemplate { get; set; }

    /// <summary>
    /// Renders each date — normally a <see cref="BitBhCalendarCell"/> with <c>Day="context"</c>. Default: a plain
    /// <see cref="BitBhCalendarCell"/>. Inside other templated content (an <c>EditForm</c>) name the context:
    /// <c>&lt;BitBhCalendarGrid Context="day"&gt;&lt;BitBhCalendarCell Day="day" /&gt;&lt;/BitBhCalendarGrid&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhCalendarDay>? ChildContent { get; set; }

    protected override string DefaultTag => "table";

    private BitBhCalendar? Owner => Context?.Owner;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(1, "id", ComponentId);
        builder.AddMultipleAttributes(2, GetFinalAttributes());
        builder.AddElementReferenceCapture(3, CaptureElement);

        if (Owner is { } owner)
        {
            var month = owner.MonthAt(Offset);

            builder.OpenElement(10, "thead");
            builder.OpenElement(11, "tr");
            foreach (var weekday in owner.Weekdays(WeekdayFormat))
            {
                builder.OpenElement(12, "th");
                builder.AddAttribute(13, "scope", "col");
                builder.AddAttribute(14, "abbr", weekday.Name);
                builder.AddAttribute(15, "aria-label", weekday.Name);
                if (WeekdayTemplate is not null) builder.AddContent(16, WeekdayTemplate(weekday));
                else builder.AddContent(17, weekday.Text);
                builder.CloseElement();
            }
            builder.CloseElement();
            builder.CloseElement();

            builder.OpenElement(20, "tbody");
            foreach (var week in owner.WeeksOf(month))
            {
                builder.OpenElement(21, "tr");
                foreach (var date in week)
                {
                    if (date is not { } d)
                    {
                        // Past the dates .NET can hold: an empty cell keeps the columns aligned.
                        builder.OpenElement(22, "td");
                        builder.CloseElement();
                        continue;
                    }
                    var day = owner.DayOf(d, month);
                    if (ChildContent is not null)
                    {
                        builder.AddContent(23, ChildContent(day));
                    }
                    else
                    {
                        builder.OpenComponent<BitBhCalendarCell>(24);
                        builder.AddComponentParameter(25, nameof(BitBhCalendarCell.Day), day);
                        builder.CloseComponent();
                    }
                }
                builder.CloseElement();
            }
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "grid";
        if (Owner is not { } owner) return attrs;

        var named = GetAdditionalAttribute("aria-label") is not null || GetAdditionalAttribute("aria-labelledby") is not null;
        if (!named)
        {
            if (owner.HasHeading && owner.EffectiveVisibleMonths == 1) attrs["aria-labelledby"] = owner.HeadingId;
            else attrs["aria-label"] = owner.System.MonthYear(owner.MonthAt(Offset));
        }
        if (owner.EffectiveMode != BitBhCalendarSelectionMode.Single) attrs["aria-multiselectable"] = "true";
        if (owner.IsReadOnly) attrs["aria-readonly"] = "true";
        if (owner.IsDisabled) attrs["aria-disabled"] = "true";
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        SetDataFlag(attrs, "readonly", owner.IsReadOnly);
        SetDataFlag(attrs, "invalid", owner.IsInvalid);
        return attrs;
    }
}
