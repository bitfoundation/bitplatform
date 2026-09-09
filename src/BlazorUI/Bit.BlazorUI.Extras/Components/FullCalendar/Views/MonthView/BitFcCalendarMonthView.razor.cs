namespace Bit.BlazorUI;

public partial class BitFcCalendarMonthView
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [Parameter] public List<BitFullCalendarEvent> SingleDayEvents { get; set; } = [];
    [Parameter] public List<BitFullCalendarEvent> MultiDayEvents { get; set; } = [];
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private readonly string _gridId = "bit-bfc-month-grid-" + Guid.NewGuid().ToString("N");

    // Roving tabindex: the month grid is a single tab stop, and the arrow keys move both the
    // tabbable cell and the focus. Without it every one of the ~42 cells would sit in the tab order.
    private List<BitFullCalendarCell> _cells = [];
    private int _columnCount = 7;
    private DateTime? _focusedDate;
    private bool _pendingCellFocus;

    /// <summary>
    /// The cell that owns the tab stop: the one the arrow keys landed on, else the first day of the
    /// month being shown, else the first rendered cell.
    /// </summary>
    private DateTime RovingDate
    {
        get
        {
            if (_focusedDate is { } focused && _cells.Any(c => c.Date.Date == focused.Date))
                return focused.Date;

            var firstOfMonth = _cells.FirstOrDefault(c => c.CurrentMonth);
            return (firstOfMonth ?? _cells.FirstOrDefault())?.Date.Date ?? DateTime.Today;
        }
    }

    private string CellButtonId(DateTime date) => $"{_gridId}-{date:yyyyMMdd}";

    private void OnCellKeyDown((DateTime Date, KeyboardEventArgs Args) payload)
    {
        // Enter and Space are the button's own activation keys, so they are left alone.
        var delta = payload.Args.Key switch
        {
            // The arrow keys follow the reading direction, which the grid flips in right-to-left.
            "ArrowRight" => State.IsRtl ? -1 : 1,
            "ArrowLeft" => State.IsRtl ? 1 : -1,
            "ArrowDown" => _columnCount,
            "ArrowUp" => -_columnCount,
            _ => 0
        };

        var index = _cells.FindIndex(c => c.Date.Date == payload.Date.Date);
        if (index < 0)
            return;

        var target = payload.Args.Key switch
        {
            // Home and End walk to the ends of the cell's own week row.
            "Home" => index - (index % _columnCount),
            "End" => index - (index % _columnCount) + _columnCount - 1,
            _ => delta == 0 ? -1 : index + delta
        };

        if (target < 0 || target >= _cells.Count)
            return;

        _focusedDate = _cells[target].Date.Date;
        _pendingCellFocus = true;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The arrow keys only moved the tabbable cell; the focus has to follow it here, once the
        // new tabindex has actually been rendered.
        if (_pendingCellFocus is false)
            return;

        _pendingCellFocus = false;
        await BitFcFocusInterop.TryFocusAsync(JS, CellButtonId(RovingDate));
    }
}
