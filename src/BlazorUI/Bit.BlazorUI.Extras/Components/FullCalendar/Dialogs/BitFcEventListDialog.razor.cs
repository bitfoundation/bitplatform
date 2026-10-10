using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitFcEventListDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }
    [Parameter] public DateTime Date { get; set; }
    [Parameter] public List<BitFullCalendarEvent> Events { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// The events in reading order: the all-day ones of the date first, then the timed ones in clock
    /// order, then by title so two events at the same minute keep a stable place.
    /// </summary>
    private List<BitFullCalendarEvent> SortedEvents => Events
        .OrderByDescending(e => e.IsAllDayOrMultiDay)
        .ThenBy(e => e.StartDate)
        .ThenBy(e => e.Title, StringComparer.Create(State.Culture, ignoreCase: true))
        .ToList();

    private bool _showDetails;
    private BitFullCalendarEvent? _selectedEvent;
    private ElementReference _dialogRef;
    private readonly string _dialogTitleId = $"bfc-list-title-{Guid.NewGuid():N}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Move focus into the dialog and trap Tab navigation once it has rendered; teardown in
        // DisposeAsync restores focus to the element that was focused before it opened.
        if (firstRender)
            await BitFcDialogInterop.SetupAsync(JS, _dialogRef);
    }

    // Whether Escape closes the dialog, the one rule OnDialogKeyDown and _EscapeClaim both go by: while the details
    // overlay is open it owns the key, and claims it on its own dialog.
    private bool _ClosesOnEscape => _showDetails is false;

    // The Escape that closes the dialog, claimed on the dialog (see Utils.claimEscape) so a surface the calendar sits
    // in does not close on the same press.
    private string? _EscapeClaim => _ClosesOnEscape ? "claim" : null;

    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        // Escape is the standard way out of a modal. Only the plain key, the one the dialog claims: an Escape with
        // a modifier is left to the surface the calendar sits in.
        if (e.IsPlainEscape() && _ClosesOnEscape)
            await OnClose.InvokeAsync();
    }

    private async Task SelectEvent(BitFullCalendarEvent ev)
    {
        if (OnEventClick.HasDelegate)
        {
            await OnEventClick.InvokeAsync(ev);
            return;
        }
        _selectedEvent = ev;
        _showDetails = true;
    }

    public async ValueTask DisposeAsync()
    {
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
