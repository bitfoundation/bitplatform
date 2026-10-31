namespace Boilerplate.Workbench.Components.Pages.Showcase.Shifts;

[BlexStore(Name = "shifts", Persist = true)]
public partial class ShiftsStore
{
    [BlexState] private BitFullCalendarView _view = BitFullCalendarView.Month;
    [BlexState] private BitFullCalendarMode _mode = BitFullCalendarMode.Timeline;
    [BlexState] private DateTime? _date;
    [BlexState] private Shift[]? _shifts;

    [BlexAction] private void OnShow(BitFullCalendarView view) => View = view;

    [BlexAction] private void OnSwitchMode(BitFullCalendarMode mode) => Mode = mode;

    [BlexAction] private void OnGoTo(DateTime date) => Date = date;

    [BlexAction] private void OnPlan(Shift[] shifts) => Shifts = shifts;
}
