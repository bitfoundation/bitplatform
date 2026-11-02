using System.Globalization;
using Bit.Bcrdt;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Reads and writes the review's Bcrdt document: threads, page statuses, flows and the agent, each in a map of its own.
/// Texts are trimmed to fixed lengths, so one entry can't make the document grow without bound.
/// </summary>
public static class WorkbenchReviewDocument
{
    public const string Id = "workbench-review";

    public const int MaxTextLength = 4000;

    public const int MaxNameLength = 60;

    public const int MaxTasks = 20;

    public const int MaxScreens = 24;

    public const int MaxLinks = 48;

    private static long lastSequence;

    /// <summary>A number taken from the clock that only grows within the process, so cursors still work after a restart.</summary>
    public static long NextSequence()
    {
        while (true)
        {
            var last = Interlocked.Read(ref lastSequence);
            var next = Math.Max(DateTimeOffset.UtcNow.UtcTicks, last + 1);

            if (Interlocked.CompareExchange(ref lastSequence, next, last) == last)
                return next;
        }
    }

    /// <summary>Reads every thread, numbered per page in the order they were made.</summary>
    public static List<WorkbenchReviewThread> ReadThreads(BcrdtMap threads)
    {
        List<WorkbenchReviewThread> result = [];

        foreach (var id in threads.Keys.ToArray())
        {
            if (threads.GetMap(id) is not { } thread)
                continue;

            var messages = thread.GetArray("messages");
            var tasks = thread.GetArray("tasks");

            result.Add(new(id,
                           0,
                           ReadString(thread, "page") ?? string.Empty,
                           ReadString(thread, "query") ?? string.Empty,
                           new(ReadString(thread, "selector") ?? "body",
                               ReadString(thread, "component"),
                               ReadString(thread, "text"),
                               ReadDouble(thread, "x"),
                               ReadDouble(thread, "y")),
                           Enum.TryParse<WorkbenchReviewStatus>(ReadString(thread, "status"), ignoreCase: true, out var status) ? status : WorkbenchReviewStatus.Open,
                           ReadTime(thread, "createdAt") ?? DateTimeOffset.MinValue,
                           ReadLong(thread, "seq"),
                           ReadBool(thread, "state"),
                           [.. ReadMaps(messages).Select(m => new WorkbenchReviewMessage(ReadBool(m, "agent"), ReadString(m, "author") ?? string.Empty, ReadString(m, "text") ?? string.Empty, ReadTime(m, "at") ?? DateTimeOffset.MinValue))],
                           [.. ReadMaps(tasks).Select(t => new WorkbenchReviewTask(ReadString(t, "text") ?? string.Empty, ReadBool(t, "done")))]));
        }

        return [.. result.GroupBy(t => t.PageKey)
                         .SelectMany(page => page.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id, StringComparer.Ordinal).Select((t, index) => t with { Number = index + 1 }))
                         .OrderBy(t => t.CreatedAt)];
    }

    public static Dictionary<string, WorkbenchPageReview> ReadPages(BcrdtMap pages)
    {
        Dictionary<string, WorkbenchPageReview> result = [];

        foreach (var key in pages.Keys.ToArray())
        {
            if (pages.GetMap(key) is not { } page)
                continue;

            result[key] = new(key,
                              Enum.TryParse<WorkbenchPageStatus>(ReadString(page, "status"), ignoreCase: true, out var status) ? status : WorkbenchPageStatus.Draft,
                              ReadString(page, "by"),
                              ReadTime(page, "at"));
        }

        return result;
    }

    public static List<WorkbenchFlow> ReadFlows(BcrdtMap flows)
    {
        List<WorkbenchFlow> result = [];

        foreach (var id in flows.Keys.ToArray())
        {
            if (flows.GetMap(id) is not { } flow)
                continue;

            result.Add(new(id,
                           ReadString(flow, "name") ?? id,
                           [.. ReadMaps(flow.GetArray("screens")).Select(ReadScreen)],
                           [.. ReadMaps(flow.GetArray("links")).Select(ReadLink)],
                           ReadTime(flow, "createdAt") ?? DateTimeOffset.MinValue));
        }

        return [.. result.OrderBy(f => f.CreatedAt)];
    }

    /// <summary>Writes a flow over the one with the same id, keeping when it was first made.</summary>
    public static void SaveFlow(BcrdtMap flows, string id, string name, IEnumerable<WorkbenchFlowScreen> screens, IEnumerable<WorkbenchFlowLink> links)
    {
        var flow = new BcrdtMap();
        var screenList = new BcrdtArray();
        var linkList = new BcrdtArray();

        flow.Set("name", Trim(name, 80));
        flow.Set("createdAt", (flows.GetMap(id) is { } existing ? ReadString(existing, "createdAt") : null) ?? DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        flow.Set("screens", screenList);
        flow.Set("links", linkList);

        foreach (var screen in screens.Take(MaxScreens))
        {
            screenList.Push(NewScreen(screen));
        }

        foreach (var link in links.Take(MaxLinks))
        {
            linkList.Push(NewLink(link));
        }

        flows.Set(id, flow);
    }

    public static bool RenameFlow(BcrdtMap flows, string flowId, string name)
    {
        if (flows.GetMap(flowId) is not { } flow)
            return false;

        flow.Set("name", Trim(name, 80));

        return true;
    }

    public static bool AddScreen(BcrdtMap flows, string flowId, WorkbenchFlowScreen screen, WorkbenchFlowLink? link)
    {
        if (flows.GetMap(flowId) is not { } flow || flow.GetArray("screens") is not { } screens || screens.Count >= MaxScreens)
            return false;

        screens.Push(NewScreen(screen));

        if (link is not null && flow.GetArray("links") is { } links && links.Count < MaxLinks)
        {
            links.Push(NewLink(link));
        }

        return true;
    }

    public static bool MoveScreen(BcrdtMap flows, string flowId, string screenId, double x, double y)
    {
        if (FindItem(flows.GetMap(flowId)?.GetArray("screens"), screenId) is not { } screen)
            return false;

        screen.Set("x", Math.Round(x));
        screen.Set("y", Math.Round(y));

        return true;
    }

    /// <summary>Removes a screen and every arrow that starts or ends at it.</summary>
    public static bool RemoveScreen(BcrdtMap flows, string flowId, string screenId)
    {
        if (flows.GetMap(flowId) is not { } flow || RemoveItem(flow.GetArray("screens"), screenId) is false)
            return false;

        if (flow.GetArray("links") is { } links)
        {
            for (var index = links.Count - 1; index >= 0; index--)
            {
                if (links.GetMap(index) is { } link && (ReadString(link, "from") == screenId || ReadString(link, "to") == screenId))
                {
                    links.Delete(index, 1);
                }
            }
        }

        return true;
    }

    public static bool AddLink(BcrdtMap flows, string flowId, WorkbenchFlowLink link)
    {
        if (flows.GetMap(flowId)?.GetArray("links") is not { } links || links.Count >= MaxLinks)
            return false;

        links.Push(NewLink(link));

        return true;
    }

    public static bool SetLinkLabel(BcrdtMap flows, string flowId, string linkId, string? label)
    {
        if (FindItem(flows.GetMap(flowId)?.GetArray("links"), linkId) is not { } link)
            return false;

        link.Set("label", string.IsNullOrWhiteSpace(label) ? null : Trim(label, 120));

        return true;
    }

    public static bool SetLinkSpot(BcrdtMap flows, string flowId, string linkId, WorkbenchFlowSpot? spot)
    {
        if (FindItem(flows.GetMap(flowId)?.GetArray("links"), linkId) is not { } link)
            return false;

        if (spot is null)
        {
            link.Delete("spotX");
            link.Delete("spotY");
            link.Delete("spotWidth");
            link.Delete("spotHeight");
        }
        else
        {
            WriteSpot(link, spot);
        }

        return true;
    }

    public static bool RemoveLink(BcrdtMap flows, string flowId, string linkId) => RemoveItem(flows.GetMap(flowId)?.GetArray("links"), linkId);

    private static WorkbenchFlowScreen ReadScreen(BcrdtMap screen)
    {
        return new(ReadString(screen, "id") ?? string.Empty,
                   ReadString(screen, "page") ?? string.Empty,
                   ReadString(screen, "query") ?? string.Empty,
                   ReadString(screen, "note"),
                   ReadBool(screen, "state"),
                   ReadDouble(screen, "x"),
                   ReadDouble(screen, "y"));
    }

    private static WorkbenchFlowLink ReadLink(BcrdtMap link)
    {
        return new(ReadString(link, "id") ?? string.Empty,
                   ReadString(link, "from") ?? string.Empty,
                   ReadString(link, "to") ?? string.Empty,
                   ReadString(link, "label"),
                   link.Get("spotWidth") is null ? null : new(ReadDouble(link, "spotX"), ReadDouble(link, "spotY"), ReadDouble(link, "spotWidth"), ReadDouble(link, "spotHeight")));
    }

    private static BcrdtMap NewScreen(WorkbenchFlowScreen screen)
    {
        var map = new BcrdtMap();

        map.Set("id", screen.Id);
        map.Set("page", screen.PageKey);
        map.Set("query", screen.Query);
        map.Set("note", Trim(screen.Note, 300));
        map.Set("state", screen.HasState);
        map.Set("x", Math.Round(screen.X));
        map.Set("y", Math.Round(screen.Y));

        return map;
    }

    private static BcrdtMap NewLink(WorkbenchFlowLink link)
    {
        var map = new BcrdtMap();

        map.Set("id", link.Id);
        map.Set("from", link.From);
        map.Set("to", link.To);
        map.Set("label", string.IsNullOrWhiteSpace(link.Label) ? null : Trim(link.Label, 120));

        if (link.Spot is { } spot)
        {
            WriteSpot(map, spot);
        }

        return map;
    }

    private static void WriteSpot(BcrdtMap link, WorkbenchFlowSpot spot)
    {
        link.Set("spotX", Math.Round(spot.X, 4));
        link.Set("spotY", Math.Round(spot.Y, 4));
        link.Set("spotWidth", Math.Round(spot.Width, 4));
        link.Set("spotHeight", Math.Round(spot.Height, 4));
    }

    private static BcrdtMap? FindItem(BcrdtArray? array, string id) => ReadMaps(array).FirstOrDefault(item => ReadString(item, "id") == id);

    private static bool RemoveItem(BcrdtArray? array, string id)
    {
        if (array is null)
            return false;

        for (var index = 0; index < array.Count; index++)
        {
            if (array.GetMap(index) is { } item && ReadString(item, "id") == id)
            {
                array.Delete(index, 1);
                return true;
            }
        }

        return false;
    }

    /// <summary>The agent last seen on the review, or null before any agent has called.</summary>
    public static WorkbenchReviewAgentInfo? ReadAgent(BcrdtMap agent)
    {
        return ReadString(agent, "name") is { } name && ReadTime(agent, "at") is { } at
            ? new(name, ReadString(agent, "thread"), at)
            : null;
    }

    public static void AddThread(BcrdtMap threads, string id, string pageKey, string query, WorkbenchReviewAnchor anchor, string author, string text, bool hasState)
    {
        var thread = new BcrdtMap();
        var messages = new BcrdtArray();

        thread.Set("page", pageKey);
        thread.Set("query", query);
        thread.Set("selector", anchor.Selector);
        thread.Set("component", anchor.Component);
        thread.Set("text", Trim(anchor.Text, 120));
        thread.Set("x", Math.Clamp(anchor.X, 0, 1));
        thread.Set("y", Math.Clamp(anchor.Y, 0, 1));
        thread.Set("status", nameof(WorkbenchReviewStatus.Open));
        thread.Set("createdAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        thread.Set("seq", NextSequence());
        thread.Set("state", hasState);
        thread.Set("messages", messages);
        thread.Set("tasks", new BcrdtArray());

        messages.Push(NewMessage(fromAgent: false, author, text));

        threads.Set(id, thread);
    }

    /// <summary>Adds a message to a thread; one from the reviewer raises the thread's sequence and reopens it if it was done.</summary>
    public static bool AddMessage(BcrdtMap threads, string id, bool fromAgent, string author, string text)
    {
        if (threads.GetMap(id) is not { } thread || thread.GetArray("messages") is not { } messages)
            return false;

        messages.Push(NewMessage(fromAgent, author, text));

        if (fromAgent is false)
        {
            thread.Set("seq", NextSequence());

            if (ReadString(thread, "status") is nameof(WorkbenchReviewStatus.Done))
            {
                thread.Set("status", nameof(WorkbenchReviewStatus.Open));
            }
        }

        return true;
    }

    /// <summary>Sets a thread's status; a reviewer reopening it raises its sequence, so the agent hears of it.</summary>
    public static bool SetStatus(BcrdtMap threads, string id, WorkbenchReviewStatus status, bool fromAgent)
    {
        if (threads.GetMap(id) is not { } thread)
            return false;

        thread.Set("status", status.ToString());

        if (fromAgent is false && status is WorkbenchReviewStatus.Open)
        {
            thread.Set("seq", NextSequence());
        }

        return true;
    }

    public static bool SetTasks(BcrdtMap threads, string id, IEnumerable<WorkbenchReviewTask> tasks)
    {
        if (threads.GetMap(id) is not { } thread)
            return false;

        var list = new BcrdtArray();

        foreach (var task in tasks.Take(MaxTasks))
        {
            var item = new BcrdtMap();
            item.Set("text", Trim(task.Text, 300));
            item.Set("done", task.Done);
            list.Push(item);
        }

        thread.Set("tasks", list);

        return true;
    }

    public static void SetPageStatus(BcrdtMap pages, string pageKey, WorkbenchPageStatus status, string by)
    {
        var page = new BcrdtMap();

        page.Set("status", status.ToString());
        page.Set("by", Trim(by, MaxNameLength));
        page.Set("at", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));

        pages.Set(pageKey, page);
    }

    /// <summary>Records the agent's name, the thread it works on and the time, which the review shows as the agent being online.</summary>
    public static void SetAgent(BcrdtMap agent, string name, string? threadId)
    {
        agent.Set("name", Trim(name, MaxNameLength));
        agent.Set("thread", threadId);
        agent.Set("at", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
    }

    private static BcrdtMap NewMessage(bool fromAgent, string author, string text)
    {
        var message = new BcrdtMap();

        message.Set("agent", fromAgent);
        message.Set("author", Trim(author, MaxNameLength));
        message.Set("text", Trim(text, MaxTextLength));
        message.Set("at", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));

        return message;
    }

    /// <summary>The maps in an array, skipping anything else.</summary>
    private static IEnumerable<BcrdtMap> ReadMaps(BcrdtArray? array)
    {
        if (array is null)
            yield break;

        for (var index = 0; index < array.Count; index++)
        {
            if (array.GetMap(index) is { } map)
                yield return map;
        }
    }

    /// <summary>Trims the text and cuts it at the given length.</summary>
    private static string? Trim(string? value, int length) => value is null ? null : value.Trim() is var trimmed && trimmed.Length > length ? trimmed[..length] : value.Trim();

    private static string? ReadString(BcrdtMap map, string key) => map.Get(key) as string;

    private static bool ReadBool(BcrdtMap map, string key) => map.Get(key) is true;

    private static double ReadDouble(BcrdtMap map, string key) => map.Get(key) is { } value ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : 0;

    private static long ReadLong(BcrdtMap map, string key) => map.Get(key) is { } value ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : 0;

    private static DateTimeOffset? ReadTime(BcrdtMap map, string key) => DateTimeOffset.TryParse(ReadString(map, key), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ? time : null;
}
