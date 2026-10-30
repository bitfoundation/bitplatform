namespace Boilerplate.Workbench.Components.Pages.Showcase.Shifts;

public partial class ShiftsPage
{
    private static readonly BitFullCalendarView[] views = [BitFullCalendarView.Day, BitFullCalendarView.Week, BitFullCalendarView.Month, BitFullCalendarView.Agenda];

    private static readonly Dictionary<ShiftRole, string> roleColors = new()
    {
        [ShiftRole.Lead] = "#8b5cf6",
        [ShiftRole.Barista] = "#0ea5e9",
        [ShiftRole.Baker] = "#f59e0b",
        [ShiftRole.Cashier] = "#22a06b"
    };

    private bool isLoading = true;
    private bool isFailed;
    private Shift[] shifts = [];
    private List<BitFullCalendarEvent> events = [];
    private IReadOnlyList<BitFullCalendarResource> resources = [];
    private IReadOnlyList<BitFullCalendarColorOption> roles = [];
    private BitFullCalendarTexts texts = new();
    private readonly BitFullCalendarSettings settings = new()
    {
        AllowEventOverlap = false,
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

    private int WeekShiftCount => WeekShifts.Count();

    private double WeekHours => WeekShifts.Sum(s => (s.End - s.Start).TotalHours);

    protected override async Task OnInitializedAsync()
    {
        texts = CreateTexts();
        roles = [.. Enum.GetValues<ShiftRole>().Select(role => new BitFullCalendarColorOption { Id = ToColorId(role), Title = Localizer[$"ShiftsRole{role}"], Value = roleColors[role] })];
        resources = [.. ShiftsService.Staff.Select(member => new BitFullCalendarResource { Id = member.NameKey, Title = Localizer[member.NameKey], Subtitle = Localizer[$"ShiftsRole{member.Role}"] })];

        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();
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

    private List<BitFullCalendarEvent> CreateEvents()
    {
        List<BitFullCalendarEvent> result = [.. shifts.Select(ToEvent)];

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

    private static string ToColorId(ShiftRole role) => role.ToString().ToLowerInvariant();
}
