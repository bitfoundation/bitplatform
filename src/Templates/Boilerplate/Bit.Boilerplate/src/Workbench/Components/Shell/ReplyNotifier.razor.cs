namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// Announces the agent's replies on this machine with a tone and a vibration, and with a system notification when
/// "Notify me" is on. It renders nothing and sits in the Workbench layout, so it works on every shell page.
/// </summary>
public partial class ReplyNotifier
{
    // Only replies written after this circuit started are announced, so a reload doesn't replay older ones.
    private readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
    // The replies announced so far, as thread id and message index.
    private readonly HashSet<string> announced = [];

    [AutoInject] private WorkbenchReviewSession session = default!;
    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private Bit.Butil.Notification notification = default!;
    [AutoInject] private Bit.Butil.WebAudio webAudio = default!;
    [AutoInject] private Bit.Butil.Navigator navigator = default!;

    protected override void OnInitialized()
    {
        session.Changed += OnSessionChanged;
    }

    private void OnSessionChanged() => InvokeAsync(Announce);

    /// <summary>
    /// Shows a notification for the latest of the new agent replies, then plays the cue once for the batch. Each step is
    /// tried on its own, since a browser may lack one of them, as Safari lacks vibration.
    /// </summary>
    private async Task Announce()
    {
        if (session.CanWrite is false)
            return;

        var replies = session.Threads
            .SelectMany(thread => thread.Messages.Select((message, index) => (Thread: thread, Message: message, Key: $"{thread.Id}:{index}")))
            .Where(reply => reply.Message.FromAgent && reply.Message.At > startedAt && announced.Add(reply.Key))
            .ToList();

        if (replies.Count == 0)
            return;

        if (store.ReplyNotifications)
        {
            var (thread, message, _) = replies[^1];
            var pageTitle = WorkbenchPageCatalog.Find(thread.PageKey) is { } page ? Localizer[page.TitleKey].Value : thread.PageKey;

            await TryAsync(async () => await notification.Show(Localizer[nameof(WorkbenchStrings.AgentRepliedOn), message.Author, pageTitle], new()
            {
                Body = message.Text,
                Tag = thread.Id,
                Icon = "/images/icons/bit-icon-512.png",
                Silent = store.IsReplySoundMuted,
                Data = $"{WorkbenchUrls.Workbench(thread.PageKey)}{thread.Query}"
            }));
        }

        if (store.IsReplySoundMuted is false)
        {
            await TryAsync(async () => await webAudio.PlayTone(880, 180));
        }

        await TryAsync(async () => await navigator.Vibrate([60, 40, 60]));
    }

    /// <summary>Runs a browser call, ignoring the error it raises where the browser can't do it.</summary>
    private static async Task TryAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (JSException)
        {
        }
    }

    public override async ValueTask DisposeAsync()
    {
        session.Changed -= OnSessionChanged;

        await base.DisposeAsync();
    }
}
