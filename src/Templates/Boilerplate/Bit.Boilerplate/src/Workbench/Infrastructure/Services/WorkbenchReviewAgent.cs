using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// What the coding agent does on the review, over MCP and HTTP: read, wait for, answer and track comments, set page statuses,
/// draw flows and take screenshots. It edits the server's copy of the review document, which syncs to every open browser.
/// </summary>
public sealed partial class WorkbenchReviewAgent
{
    /// <summary>The longest a wait for new comments lasts before it returns empty.</summary>
    public static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(55);

    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private WorkbenchScreenshotService screenshotService = default!;

    // Completed and replaced on every update of the document, which wakes the waiters.
    private TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private BcrdtDocumentHost Host => serviceProvider.GetRequiredService<BcrdtDocumentHost>();

    /// <summary>Wakes every waiter; the document host calls it for each update it receives.</summary>
    public void Pulse() => Interlocked.Exchange(ref changed, new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();

    public async Task<List<WorkbenchReviewThread>> GetThreads(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadThreads(document.GetMap("threads")), cancellationToken);
    }

    public async Task<Dictionary<string, WorkbenchPageReview>> GetPages(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadPages(document.GetMap("pages")), cancellationToken);
    }

    /// <summary>
    /// The review as the agent reads it from get_review and GET /api/review: the comments (done ones too when asked),
    /// the page statuses, the flows and the cursor to wait from.
    /// </summary>
    public async Task<object> DescribeReview(bool includeDone, CancellationToken cancellationToken)
    {
        var (threads, pages, flows) = await Host.ReadAsync(WorkbenchReviewDocument.Id, document => (WorkbenchReviewDocument.ReadThreads(document.GetMap("threads")),
                                                                                                     WorkbenchReviewDocument.ReadPages(document.GetMap("pages")),
                                                                                                     WorkbenchReviewDocument.ReadFlows(document.GetMap("flows"))), cancellationToken);

        return WorkbenchReviewJson.Describe(threads.Where(t => includeDone || t.Status is not WorkbenchReviewStatus.Done), pages, threads.Select(t => t.Sequence).DefaultIfEmpty(0).Max(), flows);
    }

    /// <summary>
    /// Returns the comments that aren't done and changed after the cursor, as soon as there are any, or none once the timeout passes.
    /// </summary>
    public async Task<(long Cursor, List<WorkbenchReviewThread> Threads)> Wait(string agentName, long since, TimeSpan timeout, CancellationToken cancellationToken)
    {
        await Touch(agentName, null, cancellationToken);

        var deadline = DateTimeOffset.UtcNow + (timeout > MaxWait ? MaxWait : timeout);

        while (true)
        {
            var signal = Volatile.Read(ref changed).Task;
            var fresh = (await GetThreads(cancellationToken)).Where(t => t.Sequence > since && t.Status is not WorkbenchReviewStatus.Done).ToList();

            if (fresh.Count > 0)
                return (fresh.Max(t => t.Sequence), fresh);

            var remaining = deadline - DateTimeOffset.UtcNow;

            if (remaining <= TimeSpan.Zero)
                return (since, []);

            // The delay's timer is cancelled as soon as the signal wins, so a busy review doesn't pile timers up.
            using (var delay = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                await Task.WhenAny(signal, Task.Delay(remaining, delay.Token));
                delay.Cancel();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public Task<bool> Reply(string agentName, string threadId, string text, CancellationToken cancellationToken)
    {
        return Edit(agentName, threadId, threads => WorkbenchReviewDocument.AddMessage(threads, threadId, fromAgent: true, agentName, text), cancellationToken);
    }

    /// <summary>Sets a comment's status; while it is working, the review shows the agent on that comment.</summary>
    public Task<bool> SetStatus(string agentName, string threadId, WorkbenchReviewStatus status, CancellationToken cancellationToken)
    {
        return Edit(agentName, status is WorkbenchReviewStatus.Working ? threadId : null, threads => WorkbenchReviewDocument.SetStatus(threads, threadId, status, fromAgent: true), cancellationToken);
    }

    public Task<bool> SetTasks(string agentName, string threadId, IEnumerable<WorkbenchReviewTask> tasks, CancellationToken cancellationToken)
    {
        return Edit(agentName, threadId, threads => WorkbenchReviewDocument.SetTasks(threads, threadId, tasks), cancellationToken);
    }

    public async Task<bool> SetPageStatus(string agentName, string pageKey, WorkbenchPageStatus status, CancellationToken cancellationToken)
    {
        if (WorkbenchPageCatalog.Find(pageKey) is not { } page)
            return false;

        await Host.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            WorkbenchReviewDocument.SetPageStatus(document.GetMap("pages"), page.Key, status, agentName);
            WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, null);
        }, cancellationToken);

        return true;
    }

    public async Task<List<WorkbenchFlow>> GetFlows(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadFlows(document.GetMap("flows")), cancellationToken);
    }

    /// <summary>Checks and draws a flow; returns its id, or what is wrong with the request.</summary>
    public async Task<(string? Id, string? Problem)> SaveFlow(string agentName, WorkbenchFlowRequest request, CancellationToken cancellationToken)
    {
        var requests = request.Screens ?? [];
        var linkRequests = request.Links ?? [];

        if (string.IsNullOrWhiteSpace(request.Name) || requests.Length == 0)
            return (null, "Give the flow a name and at least one screen.");

        if (requests.Length > WorkbenchReviewDocument.MaxScreens || linkRequests.Length > WorkbenchReviewDocument.MaxLinks)
            return (null, $"A flow holds up to {WorkbenchReviewDocument.MaxScreens} screens and {WorkbenchReviewDocument.MaxLinks} links.");

        if (requests.FirstOrDefault(screen => WorkbenchPageCatalog.Find(screen.Page) is null) is { } unknown)
            return (null, $"There is no page with the key \"{unknown.Page}\".");

        var keys = requests.Select(screen => screen.Key).ToArray();

        if (keys.Any(string.IsNullOrWhiteSpace) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            return (null, "Give every screen a key of its own.");

        if (linkRequests.FirstOrDefault(link => keys.Contains(link.From) is false || keys.Contains(link.To) is false) is { } stray)
            return (null, $"The link from \"{stray.From}\" to \"{stray.To}\" names a screen that is not in the flow.");

        // Drawing a saved flow again keeps its id, so the link to its board still works.
        var existing = request.Id is { Length: > 0 } existingId ? (await GetFlows(cancellationToken)).FirstOrDefault(f => f.Id == existingId) : null;
        var flowId = existing?.Id ?? Guid.NewGuid().ToString("N")[..16];
        var screenIds = keys.ToDictionary(key => key, _ => Guid.NewGuid().ToString("N")[..16]);
        var spots = new Dictionary<WorkbenchFlowLinkRequest, WorkbenchFlowSpot?>();
        var states = new Dictionary<string, bool>();

        // A screen with taps, or with arrows that start at a tap, is opened in the headless browser to tap through it,
        // save the page state it ends in and find where its arrows start.
        foreach (var screen in requests)
        {
            var page = WorkbenchPageCatalog.Find(screen.Page)!;
            var taps = screen.Taps?.Where(tap => string.IsNullOrWhiteSpace(tap) is false).ToArray() ?? [];
            var outgoing = linkRequests.Where(link => link.From == screen.Key && string.IsNullOrWhiteSpace(link.Tap) is false).ToArray();

            if (taps.Length == 0 && outgoing.Length == 0)
                continue;

            var capture = await screenshotService.CaptureFlowScreen(page, WorkbenchScenario.Parse(screen.Query), taps, [.. outgoing.Select(link => link.Tap!)], cancellationToken);

            if (capture.MissingTap is { } missing)
                return (null, $"Nothing to tap reads \"{missing}\" on the screen \"{screen.Key}\".");

            states[screen.Key] = await attachments.SaveState($"{flowId}{screenIds[screen.Key]}", capture.State);

            for (var index = 0; index < outgoing.Length; index++)
            {
                spots[outgoing[index]] = capture.Spots.ElementAtOrDefault(index);
            }
        }

        var positions = WorkbenchFlowLayout.Arrange([.. requests.Select(screen => (screen.Key, WorkbenchScenario.Parse(screen.Query)))],
                                                    [.. linkRequests.Select(link => (link.From, link.To))]);

        var screens = requests.Select(screen => new WorkbenchFlowScreen(screenIds[screen.Key],
                                                                        WorkbenchPageCatalog.Find(screen.Page)!.Key,
                                                                        WorkbenchScenario.Parse(screen.Query).ToQueryString(),
                                                                        screen.Note,
                                                                        states.GetValueOrDefault(screen.Key),
                                                                        positions[screen.Key].X,
                                                                        positions[screen.Key].Y));

        var links = linkRequests.Select(link => new WorkbenchFlowLink(Guid.NewGuid().ToString("N")[..16], screenIds[link.From], screenIds[link.To], link.Label, spots.GetValueOrDefault(link)));

        await Host.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            WorkbenchReviewDocument.SaveFlow(document.GetMap("flows"), flowId, request.Name, screens, links);
            WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, null);
        }, cancellationToken);

        // The screens drawn before got new ids, so their saved states are no longer used.
        foreach (var screen in existing?.Screens ?? [])
        {
            attachments.DeleteState($"{flowId}{screen.Id}");
        }

        return (flowId, null);
    }

    /// <summary>Captures the view a comment was left on, with the page state saved with it.</summary>
    public async Task<(byte[] Png, string FileName)?> Screenshot(string threadId, CancellationToken cancellationToken)
    {
        var thread = (await GetThreads(cancellationToken)).FirstOrDefault(t => t.Id == threadId);

        if (thread is null || WorkbenchPageCatalog.Find(thread.PageKey) is not { } page)
            return null;

        var snapshotId = thread.HasState ? snapshots.Add(await attachments.ReadState(thread.Id)) : null;
        var query = thread.Scenario.ToQueryString();

        return await screenshotService.CapturePage(page, snapshotId is null ? query : QueryHelpers.AddQueryString(query, WorkbenchUrls.SnapshotQueryKey, snapshotId), cancellationToken);
    }

    /// <summary>Records that the agent is online, and the comment it works on.</summary>
    private async Task Touch(string agentName, string? threadId, CancellationToken cancellationToken)
    {
        await Host.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, threadId), cancellationToken);
    }

    /// <summary>Edits the comments and records the agent in one change; returns whether the comment was found.</summary>
    private async Task<bool> Edit(string agentName, string? activeThreadId, Func<Bit.Bcrdt.BcrdtMap, bool> edit, CancellationToken cancellationToken)
    {
        var done = false;

        await Host.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            done = edit(document.GetMap("threads"));
            WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, activeThreadId);
        }, cancellationToken);

        return done;
    }
}
