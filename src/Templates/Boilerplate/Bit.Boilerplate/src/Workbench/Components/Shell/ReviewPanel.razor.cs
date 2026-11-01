using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The Review tab: the page's status, whether an agent is listening, the composer for a pinned comment and the page's
/// threads. Everything that writes shows only when the review session can write, which is in dev mode on this machine only.
/// </summary>
public partial class ReviewPanel
{
    private string? draft;
    private string? reply;
    private bool showDone;
    private bool notificationsBlocked;
    private bool isSending;
    // The thread whose delete button was pressed once and now asks to be pressed again.
    private string? confirmingDeleteId;

    [AutoInject] private WorkbenchReviewSession session = default!;
    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private Bit.Butil.Notification notification = default!;
    [AutoInject] private Bit.Butil.ServiceWorker serviceWorker = default!;
    [AutoInject] private Bit.Butil.WebAudio webAudio = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    /// <summary>The spot a new comment is being written for; while it is set, the composer shows.</summary>
    [Parameter] public WorkbenchReviewAnchor? PendingAnchor { get; set; }

    [Parameter] public string? SelectedThreadId { get; set; }

    [Parameter] public EventCallback<WorkbenchReviewThread> OnSelect { get; set; }

    /// <summary>Opens the view a comment was left on, with the page state it had.</summary>
    [Parameter] public EventCallback<WorkbenchReviewThread> OnShow { get; set; }

    [Parameter] public EventCallback<string> OnSend { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>Starts picking a spot to comment on; the phone layout sets it, the desktop uses Comment mode instead.</summary>
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
        // A selected thread that is done is only listed under Done, so switch there.
        if (SelectedThreadId is not null && PageThreads.FirstOrDefault(t => t.Id == SelectedThreadId) is { Status: WorkbenchReviewStatus.Done })
        {
            showDone = true;
        }
    }

    /// <summary>Names a comment's element by its component and text, or "this spot" when neither is known.</summary>
    private string DescribeAnchor(WorkbenchReviewAnchor anchor)
    {
        var text = string.IsNullOrWhiteSpace(anchor.Text) ? null : $"“{anchor.Text}”";

        return string.Join(" · ", new[] { anchor.Component, text }.Where(part => part is not null)) is { Length: > 0 } description
            ? description
            : Localizer[nameof(WorkbenchStrings.ThisSpot)];
    }

    /// <summary>A short line naming the view a comment was left on: its device, theme, language and data state.</summary>
    private string DescribeView(WorkbenchScenario scenario)
    {
        var device = scenario.DevicePreset;
        var parts = new List<string> { device.Key == DevicePresets.CustomKey ? FormattableString.Invariant($"{device.Width}×{device.Height}") : PreviewPane.GetDeviceName(device, Localizer) };

        if (scenario.Theme is not WorkbenchThemeMode.Light) parts.Add(Localizer[$"Theme{scenario.Theme}"]);
        if (scenario.Culture is not null) parts.Add(scenario.Culture);
        if (scenario.Data is not WorkbenchDataState.Live) parts.Add(Localizer[$"Data{scenario.Data}"]);

        return string.Join(" · ", parts);
    }

    private Task Select(WorkbenchReviewThread thread)
    {
        reply = null;
        confirmingDeleteId = null;

        return OnSelect.InvokeAsync(thread);
    }

    /// <summary>
    /// Sends the draft, which is cleared only once it is sent; the click also unlocks audio for the reply tone, and may
    /// turn reply notifications on.
    /// </summary>
    private async Task Send()
    {
        if (string.IsNullOrWhiteSpace(draft) || isSending)
            return;

        var text = draft;
        isSending = true;
        showDone = false;

        try
        {
            await UnlockAudio();
            await OnSend.InvokeAsync(text);
            draft = null;
        }
        finally
        {
            isSending = false;
        }

        await AskToNotify();
    }

    /// <summary>Unlocks audio for the reply tone, which browsers allow only after a click; a failure doesn't stop the click's work.</summary>
    private async Task UnlockAudio()
    {
        try
        {
            await webAudio.Resume();
        }
        catch (JSException)
        {
        }
    }

    /// <summary>Offers reply notifications once, after the reviewer's first comment or reply, unless the browser denied them.</summary>
    private async Task AskToNotify()
    {
        if (store.WasAskedToNotify || store.ReplyNotifications)
            return;

        store.MarkAskedToNotify();

        if (await notification.GetPermission() is Bit.Butil.NotificationPermission.Denied)
            return;

        await SetReplyNotifications(true);
    }

    /// <summary>
    /// Turning on asks the browser's permission and registers the service worker that opens a notification's page;
    /// a refusal leaves notifications off and says so.
    /// </summary>
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

    /// <summary>Ctrl+Enter, or Cmd+Enter on a Mac, sends the comment.</summary>
    private Task SendOnCtrlEnter(KeyboardEventArgs e) => IsSendKey(e) ? Send() : Task.CompletedTask;

    /// <summary>Ctrl+Enter, or Cmd+Enter on a Mac, sends the reply.</summary>
    private Task ReplyOnCtrlEnter(KeyboardEventArgs e, WorkbenchReviewThread thread) => IsSendKey(e) ? Reply(thread) : Task.CompletedTask;

    private static bool IsSendKey(KeyboardEventArgs e) => e.Key is "Enter" && (e.CtrlKey || e.MetaKey);

    private async Task Reply(WorkbenchReviewThread thread)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return;

        await UnlockAudio();
        session.Reply(thread.Id, ReviewerName, reply);
        reply = null;
        await AskToNotify();
    }

    /// <summary>Deletes a thread on the second press; the first turns the button into a question.</summary>
    private void Delete(WorkbenchReviewThread thread)
    {
        if (confirmingDeleteId != thread.Id)
        {
            confirmingDeleteId = thread.Id;
            return;
        }

        confirmingDeleteId = null;
        session.Delete(thread.Id);
    }

    private void OnSessionChanged() => InvokeAsync(StateHasChanged);

    public override async ValueTask DisposeAsync()
    {
        session.Changed -= OnSessionChanged;

        await base.DisposeAsync();
    }
}
