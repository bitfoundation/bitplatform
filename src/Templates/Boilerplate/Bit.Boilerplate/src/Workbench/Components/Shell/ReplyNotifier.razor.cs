namespace Boilerplate.Workbench.Components.Shell;

public partial class ReplyNotifier
{
    private readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
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

        await webAudio.PlayTone(880, 180);
        await navigator.Vibrate([60, 40, 60]);

        if (store.ReplyNotifications is false)
            return;

        var (thread, message, _) = replies[^1];
        var pageTitle = WorkbenchPageCatalog.Find(thread.PageKey) is { } page ? Localizer[page.TitleKey].Value : thread.PageKey;

        await notification.Show(Localizer[nameof(WorkbenchStrings.AgentRepliedOn), message.Author, pageTitle], new()
        {
            Body = message.Text,
            Tag = thread.Id,
            Icon = "/_content/Boilerplate.Client.Core/images/bit-logo.png",
            Data = $"{WorkbenchUrls.Workbench(thread.PageKey)}{thread.Query}"
        });
    }

    public override async ValueTask DisposeAsync()
    {
        session.Changed -= OnSessionChanged;

        await base.DisposeAsync();
    }
}
