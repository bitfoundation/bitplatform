namespace Boilerplate.Workbench.Components.Shell;

public partial class ReviewPanel
{
    private string? draft;
    private string? reply;
    private bool showDone;
    private bool notificationsBlocked;

    [AutoInject] private WorkbenchReviewSession session = default!;
    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private Bit.Butil.Notification notification = default!;
    [AutoInject] private Bit.Butil.ServiceWorker serviceWorker = default!;
    [AutoInject] private Bit.Butil.WebAudio webAudio = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public WorkbenchReviewAnchor? PendingAnchor { get; set; }

    [Parameter] public string? SelectedThreadId { get; set; }

    [Parameter] public EventCallback<WorkbenchReviewThread> OnSelect { get; set; }

    [Parameter] public EventCallback<WorkbenchReviewThread> OnShow { get; set; }

    [Parameter] public EventCallback<string> OnSend { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }

    [Parameter] public EventCallback OnPin { get; set; }

    [Parameter] public string ReviewerName { get; set; } = string.Empty;

    [Parameter] public EventCallback<string?> ReviewerNameChanged { get; set; }

    private WorkbenchPageStatus PageStatus => session.PageOf(Page.Key)?.Status ?? WorkbenchPageStatus.Draft;

    private IEnumerable<WorkbenchReviewThread> PageThreads => session.ThreadsOf(Page.Key);

    private IEnumerable<WorkbenchReviewThread> VisibleThreads => PageThreads.Where(t => (t.Status is WorkbenchReviewStatus.Done) == showDone);

    private int OpenCount => PageThreads.Count(t => t.Status is not WorkbenchReviewStatus.Done);

    private int DoneCount => PageThreads.Count(t => t.Status is WorkbenchReviewStatus.Done);

    private string AgentLine
    {
        get
        {
            if (session.CanWrite is false)
                return Localizer[nameof(WorkbenchStrings.AgentOffline)];

            if (session.Agent is not { IsActive: true } agent)
                return Localizer[nameof(WorkbenchStrings.NoAgentListening)];

            if (agent.ThreadId is not null && session.Threads.FirstOrDefault(t => t.Id == agent.ThreadId) is { } thread)
                return Localizer[nameof(WorkbenchStrings.AgentWorkingOn), agent.Name, thread.Number];

            return Localizer[nameof(WorkbenchStrings.AgentListening), agent.Name];
        }
    }

    protected override void OnInitialized()
    {
        session.Changed += OnSessionChanged;
    }

    protected override void OnParametersSet()
    {
        if (SelectedThreadId is not null && PageThreads.FirstOrDefault(t => t.Id == SelectedThreadId) is { Status: WorkbenchReviewStatus.Done })
        {
            showDone = true;
        }
    }

    private string DescribeAnchor(WorkbenchReviewAnchor anchor)
    {
        var text = string.IsNullOrWhiteSpace(anchor.Text) ? null : $"“{anchor.Text}”";

        return string.Join(" · ", new[] { anchor.Component, text }.Where(part => part is not null)) is { Length: > 0 } description
            ? description
            : Localizer[nameof(WorkbenchStrings.ThisSpot)];
    }

    private string DescribeView(WorkbenchScenario scenario)
    {
        var device = scenario.DevicePreset;
        var parts = new List<string> { device.Key == DevicePresets.CustomKey ? FormattableString.Invariant($"{device.Width}×{device.Height}") : device.Name };

        if (scenario.Theme is not WorkbenchThemeMode.Light) parts.Add(Localizer[$"Theme{scenario.Theme}"]);
        if (scenario.Culture is not null) parts.Add(scenario.Culture);
        if (scenario.Data is not WorkbenchDataState.Live) parts.Add(Localizer[$"Data{scenario.Data}"]);

        return string.Join(" · ", parts);
    }

    private Task Select(WorkbenchReviewThread thread)
    {
        reply = null;

        return OnSelect.InvokeAsync(thread);
    }

    private async Task Send()
    {
        if (string.IsNullOrWhiteSpace(draft))
            return;

        var text = draft;
        draft = null;
        showDone = false;

        await webAudio.Resume();
        await OnSend.InvokeAsync(text);
        await AskToNotify();
    }

    private async Task AskToNotify()
    {
        if (store.WasAskedToNotify || store.ReplyNotifications)
            return;

        store.MarkAskedToNotify();

        if (await notification.GetPermission() is Bit.Butil.NotificationPermission.Denied)
            return;

        await SetReplyNotifications(true);
    }

    private async Task SetReplyNotifications(bool on)
    {
        notificationsBlocked = false;

        if (on)
        {
            await webAudio.Resume();

            if (await notification.RequestPermission() is not Bit.Butil.NotificationPermission.Granted)
            {
                notificationsBlocked = true;
                store.SetReplyNotifications(false);
                return;
            }

            await serviceWorker.Register("/workbench-sw.js");
        }

        store.SetReplyNotifications(on);
    }

    private Task CancelPending()
    {
        draft = null;

        return OnCancel.InvokeAsync();
    }

    private async Task Reply(WorkbenchReviewThread thread)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return;

        await webAudio.Resume();
        session.Reply(thread.Id, ReviewerName, reply);
        reply = null;
        await AskToNotify();
    }

    private void Delete(WorkbenchReviewThread thread)
    {
        session.Delete(thread.Id);
    }

    private void OnSessionChanged() => InvokeAsync(StateHasChanged);

    public override async ValueTask DisposeAsync()
    {
        session.Changed -= OnSessionChanged;

        await base.DisposeAsync();
    }
}
