namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// Calls the user back when an agent needs them: a new question or a step only they can take plays a chime, unless the
/// Workbench is muted, vibrates a phone, shows a notification when "Notify me" is on, and the tab's title counts what waits.
/// A task that ends plays a shorter sound. It renders nothing and sits in the Workbench layout, so it works on every page.
/// </summary>
public partial class ProgressNotifier
{
    // Only what an agent did after this circuit started is announced, so opening the Workbench, or the review document
    // arriving in pieces as it syncs, doesn't ring for what waited before.
    private readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
    // The questions and steps waiting for the user, and the tasks that ended, seen so far.
    private readonly HashSet<string> known = [];
    private int shownCount = -1;

    [AutoInject] private WorkbenchReviewSession session = default!;
    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private Bit.Butil.Notification notification = default!;
    [AutoInject] private Bit.Butil.WebAudio webAudio = default!;
    [AutoInject] private Bit.Butil.Navigator navigator = default!;
    [AutoInject] private ILogger<ProgressNotifier> logger = default!;

    protected override void OnInitialized()
    {
        session.Changed += OnSessionChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        // The page usually opens the review document; this opens it on the pages that don't, as an agent can ask at any time.
        try
        {
            await session.Open(Localizer[nameof(WorkbenchStrings.Reviewer)]);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The review document could not be opened for the progress notifier.");
            return;
        }

        await Announce();
    }

    private void OnSessionChanged() => InvokeAsync(Announce);

    private async Task Announce()
    {
        if (session.CanSeeTasks is false || session.IsOpen is false)
            return;

        // A question carries the time it was asked; a step of the user's and a task's end take the time of the task's change.
        var waiting = session.Tasks
            .SelectMany(task => task.OpenAsks.Select(ask => (Task: task, Key: $"{task.Id}/ask/{ask.Id}", Text: ask.Question, At: ask.At))
                                .Concat(task.StepsForYou.Select(step => (Task: task, Key: $"{task.Id}/step/{step.Id}", Text: step.Text, At: task.UpdatedAt))))
            .ToList();

        var ended = session.Tasks.Where(task => task.IsFinished).Select(task => (Task: task, Key: $"{task.Id}/end/{task.Status}")).ToList();

        await ShowCount(waiting.Count);

        var fresh = waiting.Where(item => known.Add(item.Key) && item.At > startedAt).ToList();
        var finished = ended.Where(item => known.Add(item.Key) && item.Task.UpdatedAt > startedAt).ToList();

        if (fresh.Count > 0)
        {
            var (task, _, text, _) = fresh[^1];

            await Notify(Localizer[nameof(WorkbenchStrings.ProgressAgentNeedsYou), task.Agent], text, task);
            await Play((660, 120), (880, 120), (990, 200));
            await TryAsync(async () => await navigator.Vibrate([80, 60, 80, 60, 120]));
        }
        else if (finished.Count > 0)
        {
            var (task, _) = finished[^1];
            var title = task.Status is WorkbenchProgressStatus.Done
                ? Localizer[nameof(WorkbenchStrings.ProgressAgentFinished), task.Agent]
                : Localizer[nameof(WorkbenchStrings.ProgressAgentStopped), task.Agent];

            await Notify(title, task.Title, task);
            await Play(task.Status is WorkbenchProgressStatus.Done ? [(784, 110), (1046, 220)] : [(440, 160), (330, 260)]);
        }
    }

    /// <summary>Puts the number of things waiting for the user in front of the tab's title.</summary>
    private async Task ShowCount(int count)
    {
        if (count == shownCount)
            return;

        shownCount = count;

        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.setAttention", count);
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task Notify(string title, string body, WorkbenchProgressTask task)
    {
        if (store.ReplyNotifications is false)
            return;

        await TryAsync(async () => await notification.Show(title, new()
        {
            Body = body,
            Tag = $"progress-{task.Id}",
            Icon = "/images/icons/bit-icon-512.png",
            Silent = store.IsReplySoundMuted,
            Data = WorkbenchUrls.ProgressTask(task.Id)
        }));
    }

    /// <summary>Plays the notes one after the other, unless the Workbench is muted.</summary>
    private async Task Play(params (int Frequency, int Milliseconds)[] notes)
    {
        if (store.IsReplySoundMuted)
            return;

        foreach (var (frequency, milliseconds) in notes)
        {
            await TryAsync(async () => await webAudio.PlayTone(frequency, milliseconds));
            await Task.Delay(milliseconds + 30);
        }
    }

    /// <summary>Runs a browser call, ignoring the error it raises where the browser can't do it or the tab is gone.</summary>
    private static async Task TryAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public override async ValueTask DisposeAsync()
    {
        session.Changed -= OnSessionChanged;

        await base.DisposeAsync();
    }
}
