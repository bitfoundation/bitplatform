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

        try
        {
            var scrolled = await BitFcAgendaScrollInterop.TryScrollToDateAsync(JS, _scrollContainerId, DateTime.Today);
            if (scrolled)
                _lastAgendaScrollNonce = nonce;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException or InvalidOperationException)
        {
            // The circuit/render is mid-teardown, the JS side isn't reachable, or interop was issued
            // during prerender (InvalidOperationException); the scroll is a best-effort convenience,
            // so swallow the transient failure and retry on a later render (the nonce is intentionally
            // left unchanged so the scroll is re-attempted).
        }
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
