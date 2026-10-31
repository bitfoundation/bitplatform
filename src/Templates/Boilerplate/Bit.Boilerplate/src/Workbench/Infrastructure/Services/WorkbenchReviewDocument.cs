using System.Globalization;
using Bit.Bcrdt;

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchReviewDocument
{
    public const string Id = "workbench-review";

    public const int MaxTextLength = 4000;

    public const int MaxNameLength = 60;

    public const int MaxTasks = 20;

    public const int MaxSteps = 24;

    private static long lastSequence;

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
                           [.. ReadMaps(flow.GetArray("steps")).Select(step => new WorkbenchFlowStep(ReadString(step, "id") ?? string.Empty,
                                                                                                     ReadString(step, "page") ?? string.Empty,
                                                                                                     ReadString(step, "query") ?? string.Empty,
                                                                                                     ReadString(step, "note"),
                                                                                                     ReadBool(step, "state")))],
                           ReadTime(flow, "createdAt") ?? DateTimeOffset.MinValue));
        }

        return [.. result.OrderBy(f => f.CreatedAt)];
    }

    public static void AddFlow(BcrdtMap flows, string id, string name, IEnumerable<WorkbenchFlowStep> steps)
    {
        var flow = new BcrdtMap();
        var list = new BcrdtArray();

        flow.Set("name", Trim(name, 80));
        flow.Set("createdAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        flow.Set("steps", list);

        foreach (var step in steps.Take(MaxSteps))
        {
            list.Push(NewStep(step));
        }

        flows.Set(id, flow);
    }

    public static bool AddStep(BcrdtMap flows, string flowId, WorkbenchFlowStep step)
    {
        if (flows.GetMap(flowId)?.GetArray("steps") is not { } steps || steps.Count >= MaxSteps)
            return false;

        steps.Push(NewStep(step));

        return true;
    }

    public static bool RemoveStep(BcrdtMap flows, string flowId, string stepId)
    {
        if (flows.GetMap(flowId)?.GetArray("steps") is not { } steps)
            return false;

        for (var index = 0; index < steps.Count; index++)
        {
            if (steps.GetMap(index) is { } step && ReadString(step, "id") == stepId)
            {
                steps.Delete(index, 1);
                return true;
            }
        }

        return false;
    }

    private static BcrdtMap NewStep(WorkbenchFlowStep step)
    {
        var map = new BcrdtMap();

        map.Set("id", step.Id);
        map.Set("page", step.PageKey);
        map.Set("query", step.Query);
        map.Set("note", Trim(step.Note, 300));
        map.Set("state", step.HasState);

        return map;
    }

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

    private static string? Trim(string? value, int length) => value is null ? null : value.Trim() is var trimmed && trimmed.Length > length ? trimmed[..length] : value.Trim();

    private static string? ReadString(BcrdtMap map, string key) => map.Get(key) as string;

    private static bool ReadBool(BcrdtMap map, string key) => map.Get(key) is true;

    private static double ReadDouble(BcrdtMap map, string key) => map.Get(key) is { } value ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : 0;

    private static long ReadLong(BcrdtMap map, string key) => map.Get(key) is { } value ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : 0;

    private static DateTimeOffset? ReadTime(BcrdtMap map, string key) => DateTimeOffset.TryParse(ReadString(map, key), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ? time : null;
}
