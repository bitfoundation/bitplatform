using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The heading of a <see cref="BitBhCalendar"/>: the visible months as the culture writes them ("November 2026",
/// "November – December 2026"), or with <see cref="Offset"/> one of them (a title over each of several grids). With one
/// visible month it names the grid (<c>aria-labelledby</c>). Renders an <c>&lt;h2&gt;</c>; pick another level with
/// <c>As</c>. Paging with the previous / next buttons is announced through <see cref="IBitBhAnnouncer"/>, so the heading
/// itself is not a live region.
/// </summary>
public class BitBhCalendarHeading : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhCalendarContext? Context { get; set; }

    /// <summary>
    /// Shows one visible month rather than all of them: 0 for the first, 1 for the next … (see
    /// <see cref="BitBhCalendarGrid.Offset"/>). Default: all the visible months.
    /// </summary>
    [Parameter]
    public int? Offset { get; set; }

    /// <summary>Replaces the text. Use <see cref="Template"/> to render it from the calendar's state.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content rendered from the calendar's state: <c>&lt;Template Context="calendar"&gt;@calendar.Title&lt;/Template&gt;</c>.</summary>
    [Parameter]
    public RenderFragment<BitBhCalendarContext>? Template { get; set; }

    protected override string DefaultTag => "h2";

    // The heading of all the visible months is the one that names a single grid.
    private bool Names => Offset is null;

    protected override void OnInitialized()
    {
        if (Names) Context?.Owner.RegisterHeading(true);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Names && Context is { } named ? named.Owner.HeadingId : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        if (Context is { } context)
        {
            if (Template is not null || ChildContent is not null) builder.AddContent(40, BitBhContent.Pick(Template, ChildContent, context));
            else builder.AddContent(41, Offset is { } offset ? context.Owner.System.MonthYear(context.Owner.MonthAt(offset)) : context.Title);
        }
        else
        {
            builder.AddContent(42, ChildContent);
        }
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context is { } context)
        {
            SetDataFlag(attrs, "disabled", context.IsDisabled);
        }
        return attrs;
    }

    public void Dispose()
    {
        if (Names) Context?.Owner.RegisterHeading(false);
    }
}
