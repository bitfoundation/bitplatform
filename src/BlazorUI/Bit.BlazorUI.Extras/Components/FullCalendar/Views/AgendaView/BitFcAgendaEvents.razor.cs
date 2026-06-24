namespace Bit.BlazorUI;

public partial class BitFcAgendaEvents
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }

    private string _search = "";
    private bool _showDetails;
    private BitFullCalendarEvent? _selectedEvent;
    private ulong _lastAgendaScrollNonce;
    private readonly string _scrollContainerId = "bit-bfc-agenda-scroll-" + Guid.NewGuid().ToString("N");

    protected override void OnInitialized() => State.OnStateChanged += Refresh;
    private void Refresh() => InvokeAsync(StateHasChanged);
    public void Dispose() => State.OnStateChanged -= Refresh;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var nonce = State.AgendaScrollToTodayNonce;
        if (nonce == _lastAgendaScrollNonce)
            return;

        var scrolled = await BitFcAgendaScrollInterop.TryScrollToDateAsync(JS, _scrollContainerId, DateTime.Today);
        if (scrolled)
            _lastAgendaScrollNonce = nonce;
    }

    private async Task ShowDetails(BitFullCalendarEvent ev)
    {
        if (OnEventClick.HasDelegate)
        {
            await OnEventClick.InvokeAsync(ev);
            return;
        }
        _selectedEvent = ev;
        _showDetails = true;
    }
}
