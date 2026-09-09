namespace Bit.BlazorUI;

public partial class BitFcModeTabs
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;

    private static readonly BitFullCalendarMode[] _modes =
    [
        BitFullCalendarMode.Event,
        BitFullCalendarMode.Timeline
    ];

    // Unique per instance so two calendars on one page don't produce duplicate element ids.
    private readonly string _stripId = "bit-bfc-mode-tabs-" + Guid.NewGuid().ToString("N");

    private bool _pendingFocus;

    private string TabId(BitFullCalendarMode mode) => $"{_stripId}-{(int)mode}";

    /// <summary>
    /// Arrow, Home, and End move the selection along the strip - the tab pattern every toolbar
    /// follows - so a keyboard reaches both modes from a single tab stop. Enter and Space stay with
    /// the button itself, which already activates the tab it sits on.
    /// </summary>
    private void OnKeyDown(KeyboardEventArgs e, BitFullCalendarMode mode)
    {
        var index = Array.IndexOf(_modes, mode);
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
            "End" => _modes.Length - 1,
            _ => delta == 0 ? -1 : index + delta
        };

        if (target < 0 || target >= _modes.Length || target == index)
            return;

        _pendingFocus = true;
        State.SetMode(_modes[target]);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The selection moved the tab stop; the focus has to follow it here, once the new tabindex
        // has actually been rendered.
        if (_pendingFocus is false)
            return;

        _pendingFocus = false;
        await BitFcFocusInterop.TryFocusAsync(JS, TabId(State.Mode));
    }
}
