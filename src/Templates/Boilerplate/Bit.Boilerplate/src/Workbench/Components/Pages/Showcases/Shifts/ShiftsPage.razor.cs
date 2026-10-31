namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts;

public partial class ShiftsPage
{
    private static readonly BitFullCalendarView[] views = [BitFullCalendarView.Day, BitFullCalendarView.Week, BitFullCalendarView.Month, BitFullCalendarView.Agenda];

    private static readonly string[] allStaffKeys = [.. ShiftsService.Staff.Select(member => member.NameKey)];

    private static readonly ShiftRole[] allRoles = Enum.GetValues<ShiftRole>();

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
    private List<BitFullCalendarEvent> events = [];
    private IReadOnlyList<BitFullCalendarResource> resources = [];
    private IReadOnlyList<BitFullCalendarColorOption> roles = [];
    private List<BitDropdownItem<string>> staffFilterItems = [];
    private List<BitDropdownItem<ShiftRole>> roleFilterItems = [];
    private BitFullCalendarTexts texts = new();
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

    private IEnumerable<Shift> WeekShifts
    {
        get
        {
            var start = ShiftsService.WeekStart(store.Date ?? DateTime.Today);

            return shifts.Where(s => s.Start >= start && s.Start < start.AddDays(7));
        }
    }

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

        await WhenStateRestored();
        resources = CreateResources();
        await Reload();
    }

    private async Task Reload()
    {
        isLoading = true;
        isFailed = false;
        StateHasChanged();

        try
        {
            shifts = await shiftsService.GetShifts(CurrentCancellationToken);
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

    private bool IsShown(string staffKey) => store.StaffFilter is not { } staff || staff.Contains(staffKey);

    private bool IsShown(ShiftRole role) => store.RoleFilter is not { } filter || filter.Contains(role);

    private string StaffFilterText => store.StaffFilter is not { } keys || keys.Length == allStaffKeys.Length
        ? Localizer[nameof(WorkbenchStrings.ShiftsAllStaff)]
        : string.Join(", ", keys.Select(key => Localizer[key].Value));

    private string RoleFilterText => store.RoleFilter is not { } filter || filter.Length == allRoles.Length
        ? Localizer[nameof(WorkbenchStrings.ShiftsAllRoles)]
        : string.Join(", ", filter.Select(role => Localizer[$"ShiftsRole{role}"].Value));

    private List<BitFullCalendarResource> CreateResources() =>
        [.. ShiftsService.Staff.Where(member => IsShown(member.NameKey)).Select(member => new BitFullCalendarResource { Id = member.NameKey, Title = Localizer[member.NameKey], Subtitle = Localizer[$"ShiftsRole{member.Role}"] })];

    private void FilterStaff(IEnumerable<string?> keys)
    {
        store.FilterStaff([.. keys.OfType<string>()]);
        resources = CreateResources();
        events = CreateEvents();
    }

    private void FilterRoles(IEnumerable<ShiftRole> filter)
    {
        store.FilterRoles([.. filter]);
        events = CreateEvents();
    }

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

    private async Task OnChange(BitFullCalendarChangeEventArgs args)
    {
        if (args.Event.IsBackground)
            return;

        Shift[] updated = args.Kind is BitFullCalendarChangeKind.Delete
            ? [.. shifts.Where(s => s.Id != args.Event.Id)]
            : [.. shifts.Where(s => s.Id != args.Event.Id), FromEvent(args.Event)];

        shifts = updated;
        events = CreateEvents();

        await shiftsService.Save(updated, CurrentCancellationToken);
    }

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

    private Task OpenNew(BitFullCalendarEvent? slot)
    {
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

    private Task OpenExisting(BitFullCalendarEvent calendarEvent)
    {
        if (isFailed || calendarEvent.IsBackground || shifts.FirstOrDefault(s => s.Id == calendarEvent.Id) is not { } shift)
            return Task.CompletedTask;

        return editor!.Open(shift, isNew: false);
    }

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

    private async Task OnEditorSaved(Shift shift)
    {
        shifts = [.. shifts.Where(s => s.Id != shift.Id), shift];
        events = CreateEvents();

        await shiftsService.Save(shifts, CurrentCancellationToken);

        SnackBarService.Success(Localizer[nameof(WorkbenchStrings.ShiftsSaved)]);
    }

    private async Task OnEditorDeleted(Shift shift)
    {
        shifts = [.. shifts.Where(s => s.Id != shift.Id)];
        events = CreateEvents();

        await shiftsService.Save(shifts, CurrentCancellationToken);

        SnackBarService.Info(Localizer[nameof(WorkbenchStrings.ShiftsDeleted)]);
    }

    public static string RoleColor(ShiftRole role) => roleColors[role];

    private static string ToColorId(ShiftRole role) => role.ToString().ToLowerInvariant();
}
