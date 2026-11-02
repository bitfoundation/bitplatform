namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts.Data;

/// <summary>
/// Sample shifts for the planner: eight weeks around today from each person's pattern, or the user's edited copy kept
/// in the store. Loads go through the scenario state, so the data state switches reach the page.
/// </summary>
public partial class ShiftsService
{
    public const int OpeningHour = 7;

    public const int ClosingHour = 19;

    public static IReadOnlyList<ShiftStaff> Staff { get; } =
    [
        new(nameof(WorkbenchStrings.PosStaffEmma), ShiftRole.Lead, "OOC-CO-"),
        new(nameof(WorkbenchStrings.PosStaffLucas), ShiftRole.Barista, "C-OOC-O"),
        new(nameof(WorkbenchStrings.PosStaffMila), ShiftRole.Baker, "BB-BBB-"),
        new(nameof(WorkbenchStrings.PosStaffSem), ShiftRole.Cashier, "-CCM-CC"),
        new(nameof(WorkbenchStrings.PosStaffNoah), ShiftRole.Barista, "MC-CMC-"),
        new(nameof(WorkbenchStrings.PosStaffJulia), ShiftRole.Cashier, "OM-MO-M")
    ];

    private const string Source = "shifts.week";

    [AutoInject] private ShiftsStore store = default!;
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    /// <summary>The first day of the week that holds the date, by the culture's first day of the week.</summary>
    public static DateTime WeekStart(DateTime date) => date.Date.AddDays(-((7 + date.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek) % 7));

    /// <summary>Next week's stocktake day, when the store closes early: no midday or closing shifts that day.</summary>
    public static DateTime StocktakeDay => WeekStart(DateTime.Today).AddDays(9);

    /// <summary>A day off the baker takes next week, shown as a blocked day in their row.</summary>
    public static (string StaffKey, DateTime Day) TimeOff => (Staff[2].NameKey, WeekStart(DateTime.Today).AddDays(11));

    /// <summary>The user's edited shifts when the store has them, otherwise the seed.</summary>
    public Task<Shift[]> GetShifts(CancellationToken cancellationToken) =>
        scenarioState.Load(Source, () => store.Shifts ?? CreateSeed(), () => [], cancellationToken);

    /// <summary>Keeps the shifts in the store, then simulates the save's round trip.</summary>
    public Task Save(Shift[] shifts)
    {
        // The Empty state starts from no shifts, so its edits stay on the page instead of replacing the sample data.
        if (scenarioState.ResolveState(Source) is not WorkbenchDataState.Empty)
        {
            store.Plan(shifts);
        }

        return scenarioState.Save();
    }

    /// <summary>The sample shifts from three weeks back to five ahead, without the day off and the stocktake's late shifts.</summary>
    private static Shift[] CreateSeed()
    {
        var firstWeek = WeekStart(DateTime.Today).AddDays(-21);
        List<Shift> shifts = [];

        for (var day = 0; day < 56; day++)
        {
            var date = firstWeek.AddDays(day);

            for (var index = 0; index < Staff.Count; index++)
            {
                var member = Staff[index];
                var slot = member.Pattern[day % 7];

                if (slot is '-' || (member.NameKey == TimeOff.StaffKey && date == TimeOff.Day) || (date == StocktakeDay && slot is 'C' or 'M'))
                    continue;

                var (start, end, titleKey) = slot switch
                {
                    'O' => (7, 15, nameof(WorkbenchStrings.ShiftsOpening)),
                    'M' => (10, 18, nameof(WorkbenchStrings.ShiftsMidday)),
                    'B' => (7, 13, nameof(WorkbenchStrings.ShiftsBakery)),
                    _ => (11, 19, nameof(WorkbenchStrings.ShiftsClosing))
                };

                shifts.Add(new($"s{day}-{index}", member.NameKey, member.Role, date.AddHours(start), date.AddHours(end), titleKey));
            }
        }

        return [.. shifts];
    }
}
