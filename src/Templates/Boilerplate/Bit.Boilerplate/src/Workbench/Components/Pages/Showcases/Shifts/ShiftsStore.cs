namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts;

[BlexStore(Name = "shifts", Persist = true)]
public partial class ShiftsStore
{
    [BlexState] private BitFullCalendarView? _view;
    [BlexState] private BitFullCalendarMode _mode = BitFullCalendarMode.Timeline;
    [BlexState] private DateTime? _date;
    [BlexState] private Shift[]? _shifts;
    [BlexState] private Shift? _draft;
    [BlexState] private bool _isDraftNew;
    [BlexState] private string[]? _staffFilter;
    [BlexState] private ShiftRole[]? _roleFilter;

    [BlexAction] private void OnShow(BitFullCalendarView view) => View = view;

    [BlexAction] private void OnSwitchMode(BitFullCalendarMode mode) => Mode = mode;

    [BlexAction] private void OnGoTo(DateTime date) => Date = date;

    [BlexAction] private void OnPlan(Shift[] shifts) => Shifts = shifts;

    [BlexAction]
    private void OnEdit(Shift draft, bool isNew)
    {
        Draft = draft;
        IsDraftNew = isNew;
    }

    [BlexAction] private void OnCloseEditor() => Draft = null;

    [BlexAction] private void OnFilterStaff(string[] keys) => StaffFilter = keys;

    [BlexAction] private void OnFilterRoles(ShiftRole[] roles) => RoleFilter = roles;
}
