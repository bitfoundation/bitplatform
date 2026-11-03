namespace Boilerplate.LandingPage.Client.Components.Pages.Fitness;

public partial class FitnessScreens
{
    private const int WaterGoal = 8;

    private static readonly int[] weekPattern = [12, 10, 15, 11, 10, 14, 10];

    private static readonly int[] weeksPattern = [41, 55, 38, 60, 52, 64, 58, 66];

    [AutoInject] private IStringLocalizer<FitnessStrings> F = default!;

    private readonly Habit[] habits =
    [
        new(nameof(FitnessStrings.HabitSession), nameof(FitnessStrings.HabitSessionDetail), true),
        new(nameof(FitnessStrings.HabitWalk), nameof(FitnessStrings.HabitWalkDetail), true),
        new(nameof(FitnessStrings.HabitStretch), nameof(FitnessStrings.HabitStretchDetail), false),
        new(nameof(FitnessStrings.HabitSleep), nameof(FitnessStrings.HabitSleepDetail), false)
    ];

    private int water = 5;
    private bool restTomorrow;
    private string weekday = string.Empty;
    private string[] initials = [];
    private CalendarCell[] calendar = [];
    private Bar[] weekBars = [];
    private Bar[] weeksBars = [];

    private int HabitsDone => habits.Count(h => h.Done) + (water >= WaterGoal ? 1 : 0);

    private int WeekTotal => weekBars.Where(b => b.IsFuture is false).Sum(b => b.Minutes);

    private int WeeksAverage => (int)Math.Round(weeksBars.Average(b => b.Minutes));

    protected override void OnInitialized()
    {
        var today = DateTime.Today;
        var format = CurrentCulture.DateTimeFormat;
        var offset = ((int)today.DayOfWeek - (int)format.FirstDayOfWeek + 7) % 7;
        var weekStart = today.AddDays(-offset);
        var restIndex = (offset + 5) % 7;

        weekday = format.GetDayName(today.DayOfWeek);
        initials = [.. Enumerable.Range(0, 7).Select(i => format.GetShortestDayName(weekStart.AddDays(i).DayOfWeek))];

        calendar =
        [
            .. Enumerable.Range(-21, 35).Select(i =>
            {
                var date = weekStart.AddDays(i);
                var days = (date - today).Days;
                var state = days switch
                {
                    0 => "is-today is-done",
                    > 0 => "is-future",
                    _ when ((date - weekStart).Days % 7 + 7) % 7 == restIndex => "is-rest",
                    _ => "is-done"
                };

                return new CalendarCell(state, days is 1);
            })
        ];

        weekBars =
        [
            .. Enumerable.Range(-6, 7).Select(i =>
            {
                var date = today.AddDays(i);
                var index = ((date - weekStart).Days % 7 + 7) % 7;
                var isRest = index == restIndex;

                return new Bar(format.GetShortestDayName(date.DayOfWeek), format.GetDayName(date.DayOfWeek), isRest ? 0 : i is 0 ? 10 : weekPattern[index], false, isRest);
            })
        ];

        weeksBars =
        [
            .. Enumerable.Range(0, 8).Select(i =>
            {
                var start = weekStart.AddDays((i - 7) * 7);
                var label = i is 0 || i is 7 ? start.ToString("d MMM", CurrentCulture) : string.Empty;

                return new Bar(label, start.ToString("d MMMM", CurrentCulture), weeksPattern[i], false, false);
            })
        ];

        base.OnInitialized();
    }

    private void AddGlass() => water = Math.Min(water + 1, WaterGoal);

    private sealed class Habit(string name, string detail, bool done)
    {
        public string Name { get; } = name;

        public string Detail { get; } = detail;

        public bool Done { get; set; } = done;
    }

    private sealed record CalendarCell(string State, bool IsTomorrow);

    private sealed record Bar(string Label, string Name, int Minutes, bool IsFuture, bool IsRest);
}
