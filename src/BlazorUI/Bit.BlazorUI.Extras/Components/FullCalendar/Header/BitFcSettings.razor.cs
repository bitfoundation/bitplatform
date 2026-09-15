namespace Bit.BlazorUI;

public partial class BitFcSettings : IDisposable
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    private bool _open;

    private static readonly int[] _slotDurations = BitFullCalendarSettings.SlotDurations;

    // Unique per instance so multiple calendars on one page don't produce duplicate element IDs.
    private readonly string _menuId = $"bit-bfc-settings-menu-{Guid.NewGuid():N}";

    private CancellationTokenSource? _closeCts;

    private void Toggle() => _open = !_open;

    private void OnKeyDown(KeyboardEventArgs e)
    {
        // A menu that only closes by clicking its trigger again traps keyboard users; Escape is the
        // expected way out of a popup.
        if (_open && e.Key is "Escape" or "Esc")
            _open = false;
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
