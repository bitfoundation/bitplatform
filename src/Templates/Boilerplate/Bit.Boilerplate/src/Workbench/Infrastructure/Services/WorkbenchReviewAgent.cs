using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchReviewAgent
{
    public static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(55);

    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private WorkbenchScreenshotService screenshotService = default!;

    private TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private BcrdtDocumentHost Host => serviceProvider.GetRequiredService<BcrdtDocumentHost>();

    public void Pulse() => Interlocked.Exchange(ref changed, new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();

    public async Task<List<WorkbenchReviewThread>> GetThreads(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadThreads(document.GetMap("threads")), cancellationToken);
    }

    public async Task<Dictionary<string, WorkbenchPageReview>> GetPages(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadPages(document.GetMap("pages")), cancellationToken);
    }

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

            await Task.WhenAny(signal, Task.Delay(remaining, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(150, cancellationToken);
        }
    }

    public Task<bool> Reply(string agentName, string threadId, string text, CancellationToken cancellationToken)
    {
        return Edit(agentName, threadId, threads => WorkbenchReviewDocument.AddMessage(threads, threadId, fromAgent: true, agentName, text), cancellationToken);
    }

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

        var flowId = request.Id is { Length: > 0 } existingId && (await GetFlows(cancellationToken)).Any(f => f.Id == existingId) ? existingId : Guid.NewGuid().ToString("N")[..16];
        var screenIds = keys.ToDictionary(key => key, _ => Guid.NewGuid().ToString("N")[..16]);
        var spots = new Dictionary<WorkbenchFlowLinkRequest, WorkbenchFlowSpot?>();
        var states = new Dictionary<string, bool>();

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

        return (flowId, null);
    }

    public async Task<(byte[] Png, string FileName)?> Screenshot(string threadId, CancellationToken cancellationToken)
    {
        var thread = (await GetThreads(cancellationToken)).FirstOrDefault(t => t.Id == threadId);

        if (thread is null || WorkbenchPageCatalog.Find(thread.PageKey) is not { } page)
            return null;

        var snapshotId = thread.HasState ? snapshots.Add(await attachments.ReadState(thread.Id)) : null;
        var query = thread.Scenario.ToQueryString();

        return await screenshotService.CapturePage(page, snapshotId is null ? query : QueryHelpers.AddQueryString(query, WorkbenchUrls.SnapshotQueryKey, snapshotId), cancellationToken);
    }

    private async Task Touch(string agentName, string? threadId, CancellationToken cancellationToken)
    {
        await Host.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, threadId), cancellationToken);
    }

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
