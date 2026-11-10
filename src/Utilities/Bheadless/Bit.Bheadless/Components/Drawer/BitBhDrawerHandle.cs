using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The grab bar of a <see cref="BitBhDrawerPanel"/> - the visual cue that the drawer can be dragged. A drag that
/// starts on it is always a swipe, also on touch (it carries <c>touch-action: none</c>), whatever the content around
/// it does. It is decorative and hidden from assistive tech: keyboard and screen-reader users close the drawer with
/// Escape or a <see cref="BitBhDrawerClose"/>.
///
/// <para>
/// In a drawer with two or more <see cref="BitBhDrawer.SnapPoints"/> it is how the drawer's size is changed without
/// a pointer: a focusable <c>role="slider"</c> (like the grabber of an iOS sheet) whose value is the active snap point
/// - <c>aria-valuenow</c> its position from 1, <c>aria-valuetext</c> "2 of 3" (<see cref="BitBhStrings.DrawerSnapPoint"/>,
/// or your own <c>aria-valuetext</c>) - named "Drawer size" (<see cref="BitBhStrings.DrawerSize"/>) unless you give it an
/// <c>aria-label</c>. A press on it moves the drawer to the next snap point, from the largest back to the smallest
/// (Vaul's handle; see <see cref="PreventCycle"/>).
/// </para>
///
/// <para>
/// <b>Keyboard</b> (with snap points): the arrow pointing away from the drawer's edge opens it to the next snap point
/// (Up for a bottom drawer, Down for a top one, Left for a right one, Right for a left one) and the one pointing at the
/// edge closes it to the previous one; Up / Down also do that for a left or right drawer. Home and End go to the
/// smallest and the largest; Enter and Space do what a press does.
/// </para>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-side</c>; with snap points also
/// <c>data-snap-point</c> and the runtime's <c>data-focus-visible</c> / <c>data-hover</c>.</para>
/// </summary>
public class BitBhDrawerHandle : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDrawerContext? DrawerContext { get; set; }

    /// <summary>
    /// Whether a press (and Enter / Space) on the handle leaves the snap point alone; the arrow keys, Home and End still
    /// change it. Default: false - a press moves the drawer to the next snap point.
    /// </summary>
    [Parameter]
    public bool PreventCycle { get; set; }

    /// <summary>Optional content (most handles are an empty, styled bar).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>Whether the drawer has snap points to move between: the handle is then a slider.</summary>
    private bool IsAdjustable => DrawerContext is { SnapPoints.Count: > 1 };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsAdjustable)
        {
            builder.AddAttribute(21, "onclick", On<MouseEventArgs>("onclick", HandleClick));
            builder.AddAttribute(22, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        }
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-bit-bh-drawer-handle"] = "";
        SetDataState(attrs, DrawerContext?.IsOpen ?? false);
        if (DrawerContext is not null) SetDataValue(attrs, "side", DrawerContext.Side);
        BitBhDrawer.AddSnapPointAttribute(attrs, DrawerContext);
        // A press on the handle steps the snap point (or does nothing): rendered as a button, it must not submit a form.
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        if (IsAdjustable && DrawerContext is { } drawer)
        {
            // An adjustable widget: the drawer's size, one step per snap point.
            var count = drawer.SnapPoints.Count;
            var index = Math.Max(0, drawer.ActiveSnapPointIndex);
            attrs["role"] = "slider";
            attrs["tabindex"] = "0";
            attrs["aria-orientation"] = drawer.Side is "top" or "bottom" ? "vertical" : "horizontal";
            attrs["aria-valuemin"] = "1";
            attrs["aria-valuemax"] = count.ToString(CultureInfo.InvariantCulture);
            attrs["aria-valuenow"] = (index + 1).ToString(CultureInfo.InvariantCulture);
            attrs["aria-valuetext"] = Strings.DrawerSnapPoint(index, count);
            attrs["aria-controls"] = drawer.PanelId;
            if (GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
                attrs["aria-label"] = Strings.DrawerSize;
            var arrows = drawer.Side is "top" or "bottom" ? "ArrowUp ArrowDown" : "ArrowUp ArrowDown ArrowLeft ArrowRight";
            attrs[BitBhKeyboard.KeysAttribute] = PreventCycle ? $"{arrows} Home End" : $"{arrows} Home End Enter Space";
            attrs["data-bit-bh-interactive"] = "";
        }
        else
        {
            attrs["aria-hidden"] = "true";
        }
        // Functional: a touch drag on the handle is the drawer's, never a page pan.
        attrs["style"] = "touch-action:none;";
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _) => PreventCycle ? Task.CompletedTask : MoveAsync(Cycle());

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (DrawerContext is not { } drawer || !IsAdjustable) return Task.CompletedTask;
        var last = drawer.SnapPoints.Count - 1;
        var index = Math.Max(0, drawer.ActiveSnapPointIndex);
        return args.Key switch
        {
            "Home" => MoveAsync(0),
            "End" => MoveAsync(last),
            "Enter" or " " => PreventCycle ? Task.CompletedTask : MoveAsync(Cycle()),
            _ => Step(drawer.Side, args.Key) is { } step ? MoveAsync(Math.Clamp(index + step, 0, last)) : Task.CompletedTask,
        };
    }

    // The next snap point, from the largest back to the smallest.
    private int Cycle() => DrawerContext is { } drawer ? (Math.Max(0, drawer.ActiveSnapPointIndex) + 1) % drawer.SnapPoints.Count : 0;

    /// <summary>+1 for the arrow pointing away from the edge (opens the drawer further), -1 for the one pointing at it.</summary>
    private static int? Step(string side, string key) => (side, key) switch
    {
        ("bottom", "ArrowUp") or ("top", "ArrowDown") or ("left", "ArrowRight") or ("right", "ArrowLeft") => 1,
        ("bottom", "ArrowDown") or ("top", "ArrowUp") or ("left", "ArrowLeft") or ("right", "ArrowRight") => -1,
        // A horizontal slider's Up / Down still step it (APG).
        ("left" or "right", "ArrowUp") => 1,
        ("left" or "right", "ArrowDown") => -1,
        _ => null,
    };

    private Task MoveAsync(int index)
        => DrawerContext is { } drawer && index >= 0 && index < drawer.SnapPoints.Count && index != drawer.ActiveSnapPointIndex
            ? drawer.SetActiveSnapPointAsync(drawer.SnapPoints[index])
            : Task.CompletedTask;
}
