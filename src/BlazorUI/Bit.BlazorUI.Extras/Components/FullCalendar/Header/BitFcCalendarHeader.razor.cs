namespace Bit.BlazorUI;

public partial class BitFcCalendarHeader
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarSettings Settings { get; set; } = default!;
    [CascadingParameter(Name = "HideFilters")] public bool HideFilters { get; set; }
    [CascadingParameter(Name = "HideSettings")] public bool HideSettings { get; set; }
    [CascadingParameter(Name = "OnAddClick")] public EventCallback<BitFullCalendarEvent?> OnAddClick { get; set; }

    private bool _showAddDialog;

    private async Task OnAddEventClick()
    {
        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(
                State.SelectedDate,
                State.StartOfDayHour);
            await OnAddClick.InvokeAsync(draft);
        }
        else
            _showAddDialog = true;
    }

    protected override void OnInitialized() => State.OnStateChanged += Refresh;
    private void Refresh() => InvokeAsync(StateHasChanged);
    public void Dispose() => State.OnStateChanged -= Refresh;
}
