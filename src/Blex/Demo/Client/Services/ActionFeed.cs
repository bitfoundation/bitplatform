namespace Bit.Blex.Demo.Client.Services;

/// <summary>A dispatched action as captured by <see cref="ActionFeedMiddleware"/>.</summary>
/// <param name="Sequence">The manager's monotonic sequence number.</param>
/// <param name="Store">The originating store's name.</param>
/// <param name="Action">The bare action name.</param>
/// <param name="Args">Rendered <c>name: value</c> pairs from the action payload.</param>
/// <param name="At">Wall-clock time the action completed.</param>
public sealed record FeedEntry(int Sequence, string Store, string Action, string Args, DateTimeOffset At);

/// <summary>
/// A rolling window of recently dispatched actions, filled by <see cref="ActionFeedMiddleware"/>
/// and rendered by the site's live action feed. This is exactly what a real logging or analytics
/// middleware would do -- it just happens to draw to the screen instead of a log sink.
/// </summary>
public sealed class ActionFeed
{
    private const int Capacity = 50;
    private readonly List<FeedEntry> _entries = [];

    /// <summary>Raised after an entry is added or the feed is cleared.</summary>
    public event Action? Changed;

    /// <summary>The captured actions, newest first.</summary>
    public IReadOnlyList<FeedEntry> Entries => _entries;

    /// <summary>Total number of actions seen since the app started (not capped by the window).</summary>
    public int TotalSeen { get; private set; }

    internal void Add(BlexActionContext context)
    {
        TotalSeen++;
        var args = context.Args.Count == 0
            ? ""
            : string.Join(", ", context.Args.Select(a => $"{a.Name}: {Format(a.Value)}"));

        _entries.Insert(0, new FeedEntry(
            context.Sequence, context.Store.Name, context.ActionName, args, context.Timestamp));

        if (_entries.Count > Capacity)
            _entries.RemoveAt(_entries.Count - 1);

        Changed?.Invoke();
    }

    /// <summary>Empties the feed window.</summary>
    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke();
    }

    private static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        bool b => b ? "true" : "false",
        System.Collections.IEnumerable e and not string => $"[{e.Cast<object?>().Count()} items]",
        _ => value.ToString() ?? "",
    };
}

/// <summary>
/// Observing middleware: sees every action after it applies, together with its argument payload.
/// Registered with <c>options.UseMiddleware&lt;ActionFeedMiddleware&gt;()</c> so it can take
/// scoped dependencies from DI.
/// </summary>
public sealed class ActionFeedMiddleware(ActionFeed feed) : IBlexMiddleware
{
    /// <inheritdoc />
    public void OnAction(BlexActionContext context) => feed.Add(context);
}
