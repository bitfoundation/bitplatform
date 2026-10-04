namespace Bit.BlazorUI;

public partial class BitFcViewTabs
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;

    private IReadOnlyList<BitFullCalendarView> _views = [];
    private bool _pendingFocus;

    // Derived from the calendar's id, so the body can name the tab that labels it.
    private string TabId(BitFullCalendarView view) => Parts.ViewTabId(view);

    /// <summary>
    /// Arrow, Home, and End move the selection along the strip - the tab pattern every toolbar
    /// follows - so a keyboard reaches every view without one tab stop per tab. Enter and Space stay
    /// with the button itself, which already activates the tab it sits on.
    /// </summary>
    private void OnKeyDown(KeyboardEventArgs e, BitFullCalendarView view)
    {
        if (_views.Count == 0)
            return;

        var index = -1;
        for (var i = 0; i < _views.Count; i++)
        {
            if (_views[i] == view)
            {
                index = i;
                break;
            }
        }
        if (index < 0)
            return;

        // The arrows follow the reading direction, which a right-to-left layout flips.
        var delta = e.Key switch
        {
            "ArrowRight" => State.IsRtl ? -1 : 1,
            "ArrowLeft" => State.IsRtl ? 1 : -1,
            _ => 0
        };

        var target = e.Key switch
        {
            "Home" => 0,
            "End" => _views.Count - 1,
            _ => delta == 0 ? -1 : index + delta
        };

        if (target < 0 || target >= _views.Count || target == index)
            return;

        _pendingFocus = true;
        State.SetView(_views[target]);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The selection moved the tab stop; the focus has to follow it here, once the new tabindex
        // has actually been rendered.
        if (_pendingFocus is false)
            return;

        _pendingFocus = false;
        await BitFcFocusInterop.TryFocusAsync(JS, TabId(State.View));
    }
}
