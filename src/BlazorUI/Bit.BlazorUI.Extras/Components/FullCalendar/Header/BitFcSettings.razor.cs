namespace Bit.BlazorUI;

public partial class BitFcSettings : IDisposable
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    private bool _open;

    private static readonly int[] _slotDurations = BitFullCalendarSettings.SlotDurations;

    // Unique per instance so multiple calendars on one page don't produce duplicate element IDs.
    private static string NewId(string part) => $"bit-bfc-settings-{part}-{Guid.NewGuid():N}";
    private readonly string _menuId = NewId("menu");
    private readonly string _buttonId = NewId("button");
    private readonly string _titleId = NewId("title");
    private readonly string _startHourId = NewId("start-hour");
    private readonly string _slotDurationId = NewId("slot");
    private readonly string _groupById = NewId("group-by");
    private readonly string _choiceIdPrefix = NewId("choice");

    private CancellationTokenSource? _closeCts;
    private string? _pendingFocusId;

    private string ChoiceId(BitFullCalendarAgendaGroupBy value) => $"{_choiceIdPrefix}-{(int)value}";

    private void Toggle()
    {
        _open = !_open;

        // The focus moves into the panel as it opens, the way a dialog is entered. Without it a browser that does
        // not focus a clicked button (Safari) would leave the focus outside, where neither Escape nor a click
        // elsewhere - both read off the focus - could close the panel again.
        if (_open)
            _pendingFocusId = _menuId;
    }

    private void ToggleBadgeVariant() => State.SetBadgeVariant(State.BadgeVariant == BitFullCalendarBadgeVariant.Dot
        ? BitFullCalendarBadgeVariant.Colored
        : BitFullCalendarBadgeVariant.Dot);

    // The Escape that closes the panel, claimed on the dropdown and its panel (see Utils.claimEscape) - read off the
    // same state OnKeyDown decides on, so a closed panel leaves the key to whatever the calendar sits in.
    private string? _EscapeClaim => _open ? "claim" : null;

    private void OnKeyDown(KeyboardEventArgs e)
    {
        // A panel that only closes by clicking its trigger again traps keyboard users; Escape is the
        // expected way out of a popup. The focus was inside the panel that is about to go away, so it is
        // handed back to the gear rather than left to fall to the document.
        if (_open && e.Key is "Escape" or "Esc")
        {
            _open = false;
            _pendingFocusId = _buttonId;
        }
    }

    /// <summary>
    /// The agenda grouping is a radio group, a single tab stop the arrow keys move the choice within.
    /// </summary>
    private void OnChoiceKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not ("ArrowDown" or "ArrowUp" or "ArrowLeft" or "ArrowRight"))
            return;

        var next = State.AgendaModeGroupBy == BitFullCalendarAgendaGroupBy.Date
            ? BitFullCalendarAgendaGroupBy.Color
            : BitFullCalendarAgendaGroupBy.Date;
        State.SetAgendaModeGroupBy(next);
        _pendingFocusId = ChoiceId(next);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingFocusId is not { } id)
            return;

        _pendingFocusId = null;
        await BitFcFocusInterop.TryFocusAsync(JS, id);
    }

    private void OnFocusIn(FocusEventArgs _)
    {
        // Focus moved back into (or within) the menu - keep it open.
        _closeCts?.Cancel();
    }

    private void OnFocusOut(FocusEventArgs args)
    {
        // FocusEventArgs carries no relatedTarget, so a focus move to one of the menu's own controls
        // is indistinguishable from leaving it. Defer the close briefly and let OnFocusIn cancel it
        // when focus lands back inside.
        if (_open is false)
            return;

        _closeCts?.Cancel();
        _closeCts?.Dispose();
        _closeCts = new CancellationTokenSource();
        var token = _closeCts.Token;
        _ = CloseAfterDelay(token);
    }

    private async Task CloseAfterDelay(CancellationToken token)
    {
        try
        {
            await Task.Delay(120, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested || _open is false)
            return;

        _open = false;
        await InvokeAsync(StateHasChanged);
    }

    private void OnStartHourChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int val))
        {
            // Constrain to the window the grid actually renders so the textbox and the state stay in
            // sync even when the user types an hour the calendar never shows.
            val = Math.Clamp(val, State.VisibleStartHour, Math.Max(State.VisibleStartHour, State.VisibleEndHour - 1));
            State.SetStartOfDayHour(val);
            StateHasChanged();
        }
    }

    private void OnSlotDurationChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int val))
        {
            State.SetSlotDurationMinutes(val);
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        _closeCts?.Cancel();
        _closeCts?.Dispose();
    }
}
