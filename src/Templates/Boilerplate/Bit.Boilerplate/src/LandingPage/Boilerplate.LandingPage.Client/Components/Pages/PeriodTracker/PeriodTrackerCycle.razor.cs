using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerCycle
{
    private const int RingDays = 30;
    private const int ShortestCycle = 18;
    private const double TrackRadius = 44.34;

    private static readonly int[] pastCycles = [29, 27, 28];
    private static readonly string[] Flows = ["None", "Spotting", "Light", "Medium", "Heavy"];
    private static readonly string[] Moods = ["Calm", "Happy", "Low", "Irritable"];
    private static readonly string[] Symptoms = ["Cramps", "Headache", "Bloating", "Acne"];
    private static readonly DayLog Empty = new();

    [AutoInject] private IStringLocalizer<PeriodTrackerStrings> P = default!;

    private readonly Dictionary<DateTime, DayLog> logs = [];
    private readonly ElementReference[] dayRefs = new ElementReference[RingDays];
    private DateTime today;
    private DateTime cycleStart;
    private DateTime selected;
    private BitChoiceGroupItem<string>[] flowItems = [];
    private BitChoiceGroupItem<string>[] moodItems = [];

    private DateTime Start => NewStart ?? cycleStart;

    private DateTime? NewStart
    {
        get
        {
            for (var day = cycleStart.AddDays(ShortestCycle); day <= today; day = day.AddDays(1))
            {
                if (IsBleeding(day) && IsBleeding(day.AddDays(-1)) is false)
                    return day;
            }

            return null;
        }
    }

    private int[] Cycles => NewStart is { } start ? [.. pastCycles, (start - cycleStart).Days] : pastCycles;

    private int CycleCount => Cycles.Length;

    private int Average => (int)Math.Round(Cycles.Average(), MidpointRounding.AwayFromZero);

    private DateTime Predicted => Start.AddDays(Average);

    private DayLog SelectedLog => logs.TryGetValue(selected, out var log) ? log : Empty;

    private string SelectedText => P[nameof(PeriodTrackerStrings.DemoSelected),
        selected == today ? P[nameof(PeriodTrackerStrings.DemoToday)] : selected.ToString("ddd d MMM", CurrentCulture),
        DayNumber(selected).ToString(CurrentCulture)];

    private string Status
    {
        get
        {
            var flowDays = 0;

            while (flowDays < 10 && Start.AddDays(flowDays) <= today && HasFlow(Start.AddDays(flowDays)))
            {
                flowDays++;
            }

            if (today < Start.AddDays(flowDays))
                return P[nameof(PeriodTrackerStrings.DemoStatusPeriod), DayNumber(today).ToString(CurrentCulture)];

            var until = (Predicted - today).Days;

            return until switch
            {
                > 1 => P[nameof(PeriodTrackerStrings.DemoStatusIn), until.ToString(CurrentCulture)],
                1 => P[nameof(PeriodTrackerStrings.DemoStatusTomorrow)],
                0 or -1 => P[nameof(PeriodTrackerStrings.DemoStatusDue)],
                _ => P[nameof(PeriodTrackerStrings.DemoStatusLate), (-until).ToString(CurrentCulture)]
            };
        }
    }

    private string? WindowArc
    {
        get
        {
            var first = (Predicted - Start).Days - 1;

            if (first > RingDays - 1)
                return null;

            var last = Math.Min(first + 2, RingDays - 1);
            var from = Math.Max(first, 0) * 12 - 6.0;
            var to = last * 12 + 6.0;

            return FormattableString.Invariant($"M {X(from):0.##} {Y(from):0.##} A {TrackRadius} {TrackRadius} 0 {(to - from > 180 ? 1 : 0)} 1 {X(to):0.##} {Y(to):0.##}");
        }
    }

    protected override void OnInitialized()
    {
        today = DateTime.UtcNow.Date;
        cycleStart = today.AddDays(-25);
        selected = today;

        Log(0, "Medium", null, "Cramps");
        Log(1, "Heavy", "Low", "Cramps", "Bloating");
        Log(2, "Medium", null);
        Log(3, "Light", "Calm");
        Log(4, "Spotting", null);
        Log(11, "None", "Happy");
        Log(19, "None", null, "Bloating");
        Log(23, "None", "Irritable", "Headache", "Acne");

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        flowItems = [.. Flows.Select(flow => new BitChoiceGroupItem<string> { Value = flow, Text = P[$"Flow{flow}"] })];
        moodItems = [.. Moods.Select(mood => new BitChoiceGroupItem<string> { Value = mood, Text = P[$"Mood{mood}"] })];

        base.OnParametersSet();
    }

    private void Log(int day, string flow, string? mood, params string[] symptoms)
    {
        var log = new DayLog { Flow = flow, Mood = mood };
        log.Symptoms.UnionWith(symptoms);
        logs[cycleStart.AddDays(day)] = log;
    }

    private void Select(DateTime day)
    {
        selected = day < Start ? Start : day > today ? today : day;
    }

    private async Task OnDayKey(KeyboardEventArgs e, DateTime day)
    {
        var step = e.Key switch
        {
            "ArrowRight" => IsRtl ? -1 : 1,
            "ArrowLeft" => IsRtl ? 1 : -1,
            _ => 0
        };

        var target = day.AddDays(step);

        if (step is 0 || target < Start || target > today)
            return;

        selected = target;
        await dayRefs[(target - Start).Days].FocusAsync();
    }

    private void SetFlow(string? value) => Ensure(selected).Flow = value ?? "None";

    private void SetMood(string? value) => Ensure(selected).Mood = value;

    private void SetSymptom(string symptom, bool value)
    {
        var log = Ensure(selected);

        if (value)
        {
            log.Symptoms.Add(symptom);
        }
        else
        {
            log.Symptoms.Remove(symptom);
        }
    }

    private DayLog Ensure(DateTime day)
    {
        if (logs.TryGetValue(day, out var log) is false)
        {
            log = new DayLog();
            logs[day] = log;
        }

        return log;
    }

    private bool IsBleeding(DateTime day) => logs.TryGetValue(day, out var log) && log.Flow is "Light" or "Medium" or "Heavy";

    private bool HasFlow(DateTime day) => logs.TryGetValue(day, out var log) && log.Flow is not "None";

    private bool InWindow(DateTime day) => Math.Abs((day - Predicted).Days) <= 1;

    private int DayNumber(DateTime day) => (day - Start).Days + 1;

    private string DayClasses(DateTime day, DayLog? log)
    {
        var classes = new List<string>();

        if (log is { Flow: not "None" })
        {
            classes.Add($"is-{log.Flow.ToLowerInvariant()}");
        }

        if (log is not null && (log.Mood is not null || log.Symptoms.Count > 0))
        {
            classes.Add("has-notes");
        }

        if (InWindow(day))
        {
            classes.Add("is-window");
        }

        if (day == today)
        {
            classes.Add("is-today");
        }

        if (day == selected)
        {
            classes.Add("is-selected");
        }

        return string.Join(' ', classes);
    }

    private string DayLabel(DateTime day, DayLog? log)
    {
        var parts = new List<string> { P[nameof(PeriodTrackerStrings.DemoAriaDay), day.ToString("d MMMM", CurrentCulture), DayNumber(day).ToString(CurrentCulture)] };

        if (log is { Flow: not "None" })
        {
            parts.Add(P[$"Flow{log.Flow}"]);
        }

        if (log is not null && (log.Mood is not null || log.Symptoms.Count > 0))
        {
            parts.Add(P[nameof(PeriodTrackerStrings.DemoAriaNotes)]);
        }

        if (InWindow(day))
        {
            parts.Add(P[nameof(PeriodTrackerStrings.DemoAriaPredicted)]);
        }

        if (day == today)
        {
            parts.Add(P[nameof(PeriodTrackerStrings.DemoAriaToday)]);
        }

        return string.Join(P[nameof(PeriodTrackerStrings.ListSeparator)], parts);
    }

    private static string DateText(DateTime day) => day.ToString("d MMM", CurrentCulture);

    private static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static double X(double degrees) => 50 + TrackRadius * Math.Sin(degrees * Math.PI / 180);

    private static double Y(double degrees) => 50 - TrackRadius * Math.Cos(degrees * Math.PI / 180);

    private sealed class DayLog
    {
        public string Flow { get; set; } = "None";

        public string? Mood { get; set; }

        public HashSet<string> Symptoms { get; } = [];
    }
}
