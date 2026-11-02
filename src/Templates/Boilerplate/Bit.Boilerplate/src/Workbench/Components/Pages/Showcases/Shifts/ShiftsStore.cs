namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts;

/// <summary>
/// The shift planner's state the user controls: the view, mode and date, their edits to the shifts, the open editor's
/// draft and the filters. It is persisted per view, so a reload or a screenshot shows the planner as it was left.
/// </summary>
[BlexStore(Name = "shifts", Persist = true)]
public partial class ShiftsStore
{
    [BlexState] private BitFullCalendarView? _view;
    [BlexState] private BitFullCalendarMode _mode = BitFullCalendarMode.Timeline;
    [BlexState] private DateTime? _date;
    // Null until the first edit, so the page shows the seed.
    [BlexState] private Shift[]? _shifts;
    // The editor's unsaved shift; null when the editor is closed.
    [BlexState] private Shift? _draft;
    [BlexState] private bool _isDraftNew;
    // Null filters show everyone and every role.
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
