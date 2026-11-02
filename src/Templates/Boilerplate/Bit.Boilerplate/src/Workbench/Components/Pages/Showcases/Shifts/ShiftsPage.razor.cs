namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts;

/// <summary>
/// The shift planner: a BitFullCalendar timeline of shifts per team member, with staff and role filters and an editor
/// to add or change a shift. Moves, resizes and deletes on the calendar save at once.
/// </summary>
public partial class ShiftsPage
{
    private static readonly BitFullCalendarView[] views = [BitFullCalendarView.Day, BitFullCalendarView.Week, BitFullCalendarView.Month, BitFullCalendarView.Agenda];

    private static readonly string[] allStaffKeys = [.. ShiftsService.Staff.Select(member => member.NameKey)];

    private static readonly ShiftRole[] allRoles = Enum.GetValues<ShiftRole>();

    /// <summary>Each role's color, a data color for the calendar, the legend and the filters.</summary>
    private static readonly Dictionary<ShiftRole, string> roleColors = new()
    {
        [ShiftRole.Lead] = "#8b5cf6",
        [ShiftRole.Barista] = "#0ea5e9",
        [ShiftRole.Baker] = "#f59e0b",
        [ShiftRole.Cashier] = "#22a06b"
    };

    private bool isPhone;
    private ShiftEditor? editor;
    private BitFullCalendar? calendar;
    private bool isLoading = true;
    private bool isFailed;
    private Shift[] shifts = [];
    // The stored shifts the page shows or last saved, so it tells its own saves from shifts set elsewhere.
    private Shift[]? appliedShifts;
    private List<BitFullCalendarEvent> events = [];
    private IReadOnlyList<BitFullCalendarResource> resources = [];
    private IReadOnlyList<BitFullCalendarColorOption> roles = [];
    private List<BitDropdownItem<string>> staffFilterItems = [];
    private List<BitDropdownItem<ShiftRole>> roleFilterItems = [];
    private BitFullCalendarTexts texts = new();
    // Shifts stay within opening hours and may not overlap.
    private readonly BitFullCalendarSettings settings = new()
    {
        AllowEventOverlap = false,
        BusinessDays = Enum.GetValues<DayOfWeek>(),
        HighlightBusinessHours = true,
        RestrictToBusinessHours = true,
        BusinessStartHour = ShiftsService.OpeningHour,
        BusinessEndHour = ShiftsService.ClosingHour,
        VisibleStartHour = 6,
        VisibleEndHour = 21,
        SlotDurationMinutes = 30
    };

    [AutoInject] private ShiftsStore store = default!;
    [AutoInject] private ShiftsService shiftsService = default!;

    /// <summary>The shifts in the week the calendar's date falls in, for the summary under the title.</summary>
    private IEnumerable<Shift> WeekShifts
    {
        get
        {
            var start = ShiftsService.WeekStart(store.Date ?? DateTime.Today);

            return shifts.Where(s => s.Start >= start && s.Start < start.AddDays(7));
        }
    }

    /// <summary>A day on phones, where a week doesn't fit, a month elsewhere; used until the user picks a view.</summary>
    private BitFullCalendarView DefaultView => isPhone ? BitFullCalendarView.Day : BitFullCalendarView.Month;

    private int WeekShiftCount => WeekShifts.Count();

    private double WeekHours => WeekShifts.Sum(s => (s.End - s.Start).TotalHours);

    protected override async Task OnInitializedAsync()
    {
        texts = CreateTexts();
        roles = [.. Enum.GetValues<ShiftRole>().Select(role => new BitFullCalendarColorOption { Id = ToColorId(role), Title = Localizer[$"ShiftsRole{role}"], Value = roleColors[role] })];
        staffFilterItems = [.. ShiftsService.Staff.Select(member => new BitDropdownItem<string> { Value = member.NameKey, Text = Localizer[member.NameKey] })];
        roleFilterItems = [.. Enum.GetValues<ShiftRole>().Select(role => new BitDropdownItem<ShiftRole> { Value = role, Text = Localizer[$"ShiftsRole{role}"] })];
        resources = CreateResources();

        Subscribe(store);

        if (IsPrerendering)
            return;

        // The filters and the edited shifts come from the store, so they are read once it holds what the browser saved.
        await WhenStateRestored();
        resources = CreateResources();

        // Store state that comes from elsewhere, like a reset, reloads the shifts; a filter change rebuilds the rows and events.
        OwnsSubscription(store.Subscribe(() => store.Shifts, stored => OnStoredShiftsChanged(stored)));
        OwnsSubscription(store.Subscribe(() => (store.StaffFilter, store.RoleFilter), _ => OnFiltersChanged()));

        await Reload();
    }

    /// <summary>Loads the shifts and builds the calendar's events; a simulated failure shows the retry message.</summary>
    private async Task Reload()
    {
        isLoading = true;
        isFailed = false;
        StateHasChanged();

        try
        {
            shifts = await shiftsService.GetShifts(CurrentCancellationToken);
            appliedShifts = store.Shifts;
            events = CreateEvents();
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        isLoading = false;
    }

    /// <summary>Whether the staff filter shows this person; an unset filter shows everyone.</summary>
    private bool IsShown(string staffKey) => store.StaffFilter is not { } staff || staff.Contains(staffKey);

    /// <summary>Whether the role filter shows this role; an unset filter shows every role.</summary>
    private bool IsShown(ShiftRole role) => store.RoleFilter is not { } filter || filter.Contains(role);

    /// <summary>The staff filter's text: all staff, or the chosen names.</summary>
    private string StaffFilterText => store.StaffFilter is not { } keys || keys.Length == allStaffKeys.Length
        ? Localizer[nameof(WorkbenchStrings.ShiftsAllStaff)]
        : string.Join(", ", keys.Select(key => Localizer[key].Value));

    /// <summary>The role filter's text: all roles, or the chosen ones.</summary>
    private string RoleFilterText => store.RoleFilter is not { } filter || filter.Length == allRoles.Length
        ? Localizer[nameof(WorkbenchStrings.ShiftsAllRoles)]
        : string.Join(", ", filter.Select(role => Localizer[$"ShiftsRole{role}"].Value));

    /// <summary>The calendar's rows: the team members the staff filter shows.</summary>
    private List<BitFullCalendarResource> CreateResources() =>
        [.. ShiftsService.Staff.Where(member => IsShown(member.NameKey)).Select(member => new BitFullCalendarResource { Id = member.NameKey, Title = Localizer[member.NameKey], Subtitle = Localizer[$"ShiftsRole{member.Role}"] })];

    private void FilterStaff(IEnumerable<string?> keys) => store.FilterStaff([.. keys.OfType<string>()]);

    private void FilterRoles(IEnumerable<ShiftRole> filter) => store.FilterRoles([.. filter]);

    /// <summary>Reloads the shifts when the store got them from elsewhere, like a reset; the page's own saves are skipped.</summary>
    private void OnStoredShiftsChanged(Shift[]? stored)
    {
        if (ReferenceEquals(stored, appliedShifts))
            return;

        appliedShifts = stored;

        _ = InvokeAsync(Reload);
    }

    /// <summary>Rebuilds the rows and events for the filters the store holds.</summary>
    private void OnFiltersChanged()
    {
        _ = InvokeAsync(() =>
        {
            resources = CreateResources();
            events = CreateEvents();
            StateHasChanged();
        });
    }

    /// <summary>The calendar's events: the filtered shifts, plus the stocktake and the day off as blocking backgrounds.</summary>
    private List<BitFullCalendarEvent> CreateEvents()
    {
        List<BitFullCalendarEvent> result = [.. shifts.Where(shift => IsShown(shift.StaffKey) && IsShown(shift.Role)).Select(ToEvent)];

        if (shifts.Length == 0)
            return result;

        var stocktake = ShiftsService.StocktakeDay;
        var (staffKey, day) = ShiftsService.TimeOff;

        result.Add(new()
        {
            Id = "stocktake",
            Title = Localizer[nameof(WorkbenchStrings.ShiftsStocktake)],
            StartDate = stocktake.AddHours(15),
            EndDate = stocktake.AddHours(ShiftsService.ClosingHour),
            Color = "#94a3b8",
            IsBackground = true,
            IsBlocking = true
        });

        if (IsShown(staffKey) is false)
            return result;

        result.Add(new()
        {
            Id = "time-off",
            Title = Localizer[nameof(WorkbenchStrings.ShiftsTimeOff)],
            StartDate = day,
            EndDate = day,
            IsAllDay = true,
            Resource = staffKey,
            Color = "#94a3b8",
            IsBackground = true,
            IsBlocking = true
        });

        return result;
    }

    /// <summary>A shift as a calendar event, colored by its role's color option.</summary>
    private BitFullCalendarEvent ToEvent(Shift shift) => new()
    {
        Id = shift.Id,
        Title = shift.Title ?? (shift.TitleKey is null ? string.Empty : Localizer[shift.TitleKey]),
        Description = Localizer[$"ShiftsRole{shift.Role}"],
        StartDate = shift.Start,
        EndDate = shift.End,
        Color = ToColorId(shift.Role),
        Resource = shift.StaffKey
    };

    /// <summary>Saves a shift the user moved, resized or deleted on the calendar.</summary>
    private async Task OnChange(BitFullCalendarChangeEventArgs args)
    {
        if (args.Event.IsBackground)
            return;

        Shift[] updated = args.Kind is BitFullCalendarChangeKind.Delete
            ? [.. shifts.Where(s => s.Id != args.Event.Id)]
            : [.. shifts.Where(s => s.Id != args.Event.Id), FromEvent(args.Event)];

        shifts = updated;
        appliedShifts = updated;
        events = CreateEvents();

        await shiftsService.Save(updated);
    }

    /// <summary>A calendar event the user moved or edited, as a shift; the sample title key stays while the title reads the same.</summary>
    private Shift FromEvent(BitFullCalendarEvent calendarEvent)
    {
        var previous = shifts.FirstOrDefault(s => s.Id == calendarEvent.Id);
        var role = Enum.TryParse<ShiftRole>(calendarEvent.Color, ignoreCase: true, out var parsed) ? parsed : previous?.Role ?? ShiftRole.Barista;
        var keptTitle = previous?.TitleKey is { } key && calendarEvent.Title == Localizer[key].Value;

        return new(string.IsNullOrEmpty(calendarEvent.Id) ? Guid.NewGuid().ToString("N") : calendarEvent.Id,
                   calendarEvent.Resource ?? previous?.StaffKey ?? ShiftsService.Staff[0].NameKey,
                   role,
                   calendarEvent.StartDate,
                   calendarEvent.EndDate,
                   keptTitle ? previous!.TitleKey : null,
                   keptTitle ? null : calendarEvent.Title);
    }

    /// <summary>The calendar's texts from the Calendar resources; a text without a resource keeps the calendar's own.</summary>
    private BitFullCalendarTexts CreateTexts()
    {
        var calendarTexts = new BitFullCalendarTexts();

        foreach (var property in typeof(BitFullCalendarTexts).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(string) && p.CanWrite))
        {
            var text = Localizer[$"Calendar{property.Name}"];

            if (text.ResourceNotFound is false)
            {
                property.SetValue(calendarTexts, text.Value);
            }
        }

        return calendarTexts;
    }

    private void OnPhoneChanged(bool phone)
    {
        isPhone = phone;
    }

    /// <summary>Opens the editor to add a shift in the slot, inside opening hours, four hours long unless the slot sets an end.</summary>
    private Task OpenNew(BitFullCalendarEvent? slot)
    {
        if (isLoading || isFailed)
            return Task.CompletedTask;

        var staff = ShiftsService.Staff.FirstOrDefault(member => member.NameKey == slot?.Resource) ?? ShiftsService.Staff[0];
        var start = slot?.StartDate ?? (store.Date ?? DateTime.Today).Date;

        if (start.TimeOfDay < TimeSpan.FromHours(ShiftsService.OpeningHour))
        {
            start = start.Date.AddHours(ShiftsService.OpeningHour);
        }

        var end = slot is { IsAllDay: false } && slot.EndDate.Date == start.Date && slot.EndDate - start >= TimeSpan.FromHours(1) ? slot.EndDate : start.AddHours(4);
        var closing = start.Date.AddHours(ShiftsService.ClosingHour);

        return editor!.Open(new(Guid.NewGuid().ToString("N"), staff.NameKey, staff.Role, start, end > closing ? closing : end), isNew: true);
    }

    /// <summary>Opens the editor on a clicked shift; background events and a failed load open nothing.</summary>
    private Task OpenExisting(BitFullCalendarEvent calendarEvent)
    {
        if (isFailed || calendarEvent.IsBackground || shifts.FirstOrDefault(s => s.Id == calendarEvent.Id) is not { } shift)
            return Task.CompletedTask;

        return editor!.Open(shift, isNew: false);
    }

    /// <summary>The calendar's reason to refuse a shift the editor would save, as a message; null when it fits.</summary>
    private string? ValidateShift(Shift shift)
    {
        return (calendar?.State.ValidateRange(shift.Id, shift.Start, shift.End, shift.StaffKey) ?? BitFullCalendarChangeRefusal.None) switch
        {
            BitFullCalendarChangeRefusal.None => null,
            BitFullCalendarChangeRefusal.OutsideBusinessHours => texts.OutsideBusinessHoursMessage,
            BitFullCalendarChangeRefusal.Overlap => texts.EventOverlapMessage,
            BitFullCalendarChangeRefusal.OutOfRange => texts.OutOfRangeMessage,
            _ => texts.BlockedMessage
        };
    }

    /// <summary>Keeps the shift the editor saved and confirms it.</summary>
    private async Task OnEditorSaved(Shift shift)
    {
        shifts = [.. shifts.Where(s => s.Id != shift.Id), shift];
        appliedShifts = shifts;
        events = CreateEvents();

        await shiftsService.Save(shifts);

        SnackBarService.Success(Localizer[nameof(WorkbenchStrings.ShiftsSaved)]);
    }

    /// <summary>Drops the shift the editor deleted and says so.</summary>
    private async Task OnEditorDeleted(Shift shift)
    {
        shifts = [.. shifts.Where(s => s.Id != shift.Id)];
        appliedShifts = shifts;
        events = CreateEvents();

        await shiftsService.Save(shifts);

        SnackBarService.Info(Localizer[nameof(WorkbenchStrings.ShiftsDeleted)]);
    }

    /// <summary>A role's color, for the role dots in the filters and the editor.</summary>
    public static string RoleColor(ShiftRole role) => roleColors[role];

    /// <summary>The id of a role's color option on the calendar.</summary>
    private static string ToColorId(ShiftRole role) => role.ToString().ToLowerInvariant();
}
