using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The shared behaviour of <see cref="BitBhCalendarPrevious"/> and <see cref="BitBhCalendarNext"/>: a button that pages
/// the visible months of a <see cref="BitBhCalendar"/> by <see cref="BitBhCalendar.VisibleMonths"/>, keeping the focus on
/// itself (the new months are announced). At the edge of <c>Min</c> / <c>Max</c> it is <c>aria-disabled</c> rather than
/// <c>disabled</c>, so the focus isn't lost when it gets there.
/// </summary>
public abstract class BitBhCalendarNavButton : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhCalendarContext? Context { get; set; }

    /// <summary>The content (an icon or text). The accessible name defaults to "Previous month" / "Next month".</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    /// <summary>Whether the button pages forward.</summary>
    private protected abstract bool Forward { get; }

    private bool CanPage => Context is { } context && (Forward ? context.CanGoNext : context.CanGoPrevious);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);
        var calendarDisabled = Context?.IsDisabled ?? false;
        if (isButton) attrs["type"] = "button";
        else
        {
            attrs["role"] = "button";
            if (!calendarDisabled) attrs["tabindex"] = "0";
        }
        attrs["aria-label"] = Forward ? Strings.NextMonth : Strings.PreviousMonth;
        if (calendarDisabled)
        {
            // The whole calendar is disabled: the buttons are too, out of the tab order like it.
            if (isButton) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
        }
        else if (!CanPage)
        {
            attrs["aria-disabled"] = "true";
        }
        SetDataFlag(attrs, "disabled", calendarDisabled || !CanPage);
        return attrs;
    }

    private Task HandleClick()
    {
        if (Context is not { } context || !CanPage) return Task.CompletedTask;
        return Forward ? context.GoNextAsync() : context.GoPreviousAsync();
    }
}

/// <summary>
/// The button that shows the previous month(s) of a <see cref="BitBhCalendar"/>, named "Previous month"
/// (<see cref="BitBhStrings.PreviousMonth"/>; an <c>aria-label</c> of your own wins).
/// </summary>
public class BitBhCalendarPrevious : BitBhCalendarNavButton
{
    private protected override bool Forward => false;
}

/// <summary>
/// The button that shows the next month(s) of a <see cref="BitBhCalendar"/>, named "Next month"
/// (<see cref="BitBhStrings.NextMonth"/>; an <c>aria-label</c> of your own wins).
/// </summary>
public class BitBhCalendarNext : BitBhCalendarNavButton
{
    private protected override bool Forward => true;
}
