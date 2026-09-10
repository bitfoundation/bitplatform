using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitFcEventListDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
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

    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        // Escape is the standard way out of a modal. While the details overlay is open it owns the key.
        if (e.Key is "Escape" or "Esc" && _showDetails is false)
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
