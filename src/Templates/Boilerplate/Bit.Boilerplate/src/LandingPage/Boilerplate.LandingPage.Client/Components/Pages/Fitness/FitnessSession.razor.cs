namespace Boilerplate.LandingPage.Client.Components.Pages.Fitness;

public partial class FitnessSession
{
    private static readonly (string Name, string Cue, int Seconds)[] moves =
    [
        (nameof(FitnessStrings.Move1), nameof(FitnessStrings.Move1Cue), 90),
        (nameof(FitnessStrings.Move2), nameof(FitnessStrings.Move2Cue), 90),
        (nameof(FitnessStrings.Move3), nameof(FitnessStrings.Move3Cue), 60),
        (nameof(FitnessStrings.Move4), nameof(FitnessStrings.Move4Cue), 120),
        (nameof(FitnessStrings.Move5), nameof(FitnessStrings.Move5Cue), 90),
        (nameof(FitnessStrings.Move6), nameof(FitnessStrings.Move6Cue), 150)
    ];

    private static readonly int total = moves.Sum(m => m.Seconds);

    [AutoInject] private IStringLocalizer<FitnessStrings> F = default!;

    private int elapsed;
    private int streak = 41;
    private bool isRunning;
    private bool isDone;
    private bool streakBumped;
    private bool countedToday;
    private bool countedTwice;
    private string? announcement;
    private string todayLabel = string.Empty;
    private Day[] week = [];
    private CancellationTokenSource? cts;

    private int CurrentIndex
    {
        get
        {
            var end = 0;

            for (var i = 0; i < moves.Length; i++)
            {
                end += moves[i].Seconds;

                if (elapsed < end)
                    return i;
            }

            return moves.Length - 1;
        }
    }

    private (string Name, string Cue, int Seconds) Current => moves[CurrentIndex];

    private int MoveRemaining => MoveEnd(CurrentIndex) - elapsed;

    private int Remaining => total - elapsed;

    // Written into the SVG, which needs a dot for decimals where Persian would use its own separator
    private string LapOffset => (100 - elapsed * 100.0 / total).ToString("0.##", CultureInfo.InvariantCulture);

    private string RingOffset => LapOffset;

    private string RingLabel => isDone ? nameof(FitnessStrings.SessionRingDone)
        : isRunning ? nameof(FitnessStrings.SessionRingLeft)
        : elapsed > 0 ? nameof(FitnessStrings.SessionRingPaused)
        : nameof(FitnessStrings.SessionRingReady);

    protected override void OnInitialized()
    {
        todayLabel = DateTime.Today.ToString("dddd d MMMM", CurrentCulture);
        week = BuildWeek();

        base.OnInitialized();
    }

    private void Start()
    {
        if (isRunning || isDone)
            return;

        isRunning = true;
        announcement = F[nameof(FitnessStrings.SessionAnnounceMove), F[Current.Name], Clock(MoveRemaining)];
        cts = new();
        _ = Run(cts.Token);
    }

    private async Task Run(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                MoveTo(elapsed + 1);
                await InvokeAsync(StateHasChanged);

                if (isDone)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Pause()
    {
        Stop();
        announcement = F[nameof(FitnessStrings.SessionAnnouncePaused)];
    }

    private void NextMove() => MoveTo(MoveEnd(CurrentIndex));

    private void MoveTo(int seconds)
    {
        var before = CurrentIndex;
        elapsed = Math.Min(seconds, total);

        if (elapsed >= total)
        {
            Finish();
        }
        else if (CurrentIndex != before)
        {
            announcement = F[nameof(FitnessStrings.SessionAnnounceMove), F[Current.Name], Clock(MoveRemaining)];
        }
    }

    private void Finish()
    {
        Stop();
        isDone = true;
        countedTwice = countedToday;

        if (countedToday is false)
        {
            countedToday = true;
            streak++;
            streakBumped = true;
            week = BuildWeek();
        }

        announcement = countedTwice ? F[nameof(FitnessStrings.SessionDoneAgain), streak] : F[nameof(FitnessStrings.SessionDoneBody), streak];
    }

    private void Reset()
    {
        Stop();
        elapsed = 0;
        isDone = false;
        announcement = null;
    }

    private void Stop()
    {
        isRunning = false;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private Day[] BuildWeek()
    {
        var today = DateTime.Today;
        var format = CurrentCulture.DateTimeFormat;
        var offset = ((int)today.DayOfWeek - (int)format.FirstDayOfWeek + 7) % 7;
        var start = today.AddDays(-offset);

        return
        [
            .. Enumerable.Range(0, 7).Select(i =>
            {
                var date = start.AddDays(i);
                var (state, text) = i < offset
                    ? offset - i == 2 ? ("is-rest", nameof(FitnessStrings.DayRest)) : ("is-done", nameof(FitnessStrings.DayDone))
                    : i == offset
                        ? countedToday ? ("is-today is-done", nameof(FitnessStrings.DayDone)) : ("is-today", nameof(FitnessStrings.DayToday))
                        : ("is-future", nameof(FitnessStrings.DayUpcoming));

                return new Day(format.GetShortestDayName(date.DayOfWeek), format.GetDayName(date.DayOfWeek), state, text);
            })
        ];
    }

    private string SegmentFill(int index)
    {
        var start = MoveEnd(index) - moves[index].Seconds;
        var fill = Math.Clamp((elapsed - start) * 100.0 / moves[index].Seconds, 0, 100);

        return fill.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static int MoveEnd(int index) => moves.Take(index + 1).Sum(m => m.Seconds);

    private static string Clock(int seconds) => string.Format(CurrentCulture, "{0}:{1:00}", seconds / 60, seconds % 60);

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private sealed record Day(string Initial, string Name, string State, string StateText);
}
