namespace Boilerplate.Workbench.Components.Pages.Showcase.Shifts.Data;

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

    [AutoInject] private ShiftsStore store = default!;
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    public static DateTime WeekStart(DateTime date) => date.Date.AddDays(-((7 + date.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek) % 7));

    public static DateTime StocktakeDay => WeekStart(DateTime.Today).AddDays(9);

    public static (string StaffKey, DateTime Day) TimeOff => (Staff[2].NameKey, WeekStart(DateTime.Today).AddDays(11));

    public Task<Shift[]> GetShifts(CancellationToken cancellationToken) =>
        scenarioState.Load("shifts.week", () => store.Shifts ?? CreateSeed(), () => [], cancellationToken);

    public async Task Save(Shift[] shifts, CancellationToken cancellationToken)
    {
        await scenarioState.Save(cancellationToken);

        store.Plan(shifts);
    }

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
