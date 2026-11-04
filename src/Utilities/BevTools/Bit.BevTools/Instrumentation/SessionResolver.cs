
namespace Bit.BevTools;

/// <summary>
/// Maps ambient execution context to a BevTools session.
/// <para>
/// Resolution order: (1) an explicit ambient session pushed by <see cref="BevToolsSession.UseAmbient"/>;
/// (2) the session whose renderer synchronization context is current — an O(1) lookup, which is the case for
/// everything that runs on a Blazor dispatcher; (3) a scan comparing <c>Dispatcher.CheckAccess()</c>; (4) the session
/// whose UI event or navigation the current <c>Activity</c> descends from, for work that left the dispatcher after it
/// began there (a library awaiting with <c>ConfigureAwait(false)</c>); (5) for single-user hosts (WebAssembly, WebView,
/// tests) the only live session.
/// </para>
/// <para>
/// The last fallback deliberately excludes every session of a server process (<see cref="BevToolsSession.IsInMultiUserHost"/>):
/// attributing ambient work to "the only circuit" would mis-attribute background/server activity to whichever user
/// happened to be connected.
/// </para>
/// </summary>
internal static class SessionResolver
{
    private static readonly object Lock = new();
    private static readonly List<WeakReference<BevToolsSession>> Sessions = [];
    private static readonly AsyncLocal<BevToolsSession?> Ambient = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SynchronizationContext, WeakReference<BevToolsSession>> ByContext = new();
    private static BevToolsSession?[] _cache = [];
    private static bool _allInMultiUserHost;

    public static void Register(BevToolsSession session)
    {
        lock (Lock)
        {
            Sessions.Add(new WeakReference<BevToolsSession>(session));
            Prune();
        }
    }

    public static void Unregister(BevToolsSession session)
    {
        lock (Lock)
        {
            Sessions.RemoveAll(w => !w.TryGetTarget(out var s) || ReferenceEquals(s, session));
            Prune();
        }
    }

    /// <summary>Records the synchronization context a session's renderer runs on, so later lookups are O(1).</summary>
    public static void NoteCurrentContext(BevToolsSession session)
    {
        if (session.SynchronizationContextNoted)
        {
            return;
        }

        var context = SynchronizationContext.Current;
        if (context is null)
        {
            return;
        }

        session.SynchronizationContextNoted = true;
        ByContext.AddOrUpdate(context, new WeakReference<BevToolsSession>(session));
    }

    public static int LiveSessionCount => Volatile.Read(ref _cache).Length;

    private static readonly AsyncLocal<bool> OwnWork = new();

    /// <summary>
    /// Marks the current async flow as BevTools' own work until disposed: application code BevTools calls into - an
    /// <c>AuthenticationStateProvider</c> asked for the current state - can make requests, log and raise activities, and
    /// none of that is application activity. Nothing resolves a session meanwhile, so none of it is recorded.
    /// </summary>
    public static IDisposable BeginBevToolsWork()
    {
        var previous = OwnWork.Value;
        OwnWork.Value = true;
        return new OwnWorkScope(previous);
    }

    public static BevToolsSession? Resolve()
    {
        if (OwnWork.Value)
        {
            return null;
        }

        var ambient = Ambient.Value;
        if (ambient is { IsDisposed: false })
        {
            return ambient;
        }

        if (SynchronizationContext.Current is { } context
            && ByContext.TryGetValue(context, out var reference)
            && reference.TryGetTarget(out var byContext)
            && !byContext.IsDisposed)
        {
            return byContext;
        }

        var sessions = Volatile.Read(ref _cache);
        if (sessions.Length == 0)
        {
            return null;
        }

        if (SynchronizationContext.Current is null && Volatile.Read(ref _allInMultiUserHost))
        {
            // A server's dispatchers say yes only on their own synchronization context: without one (the request pipeline,
            // a hosted service) none can, and a busy server would ask every circuit's on each entry it logs.
            return ActivityObserver.SessionOfCurrentActivity() is { IsDisposed: false } fromActivity ? fromActivity : null;
        }

        BevToolsSession? single = null;
        var liveCount = 0;
        foreach (var session in sessions)
        {
            if (session is null || session.IsDisposed)
            {
                continue;
            }

            liveCount++;
            single = session;
            try
            {
                if (session.Dispatcher?.CheckAccess() == true)
                {
                    return session;
                }
            }
            catch
            {
                // dispatcher of a disposed renderer; ignore.
            }
        }

        // Evidence, not a guess: the activity flowed here from that session's own event.
        if (liveCount > 0 && ActivityObserver.SessionOfCurrentActivity() is { IsDisposed: false } byActivity)
        {
            return byActivity;
        }

        // Single-user hosts (WebAssembly, WebView, tests) have no other candidate. A server process is excluded on
        // purpose, whatever its sessions report about themselves: "the only circuit right now" is not evidence that
        // this activity belongs to that user.
        return liveCount == 1 && !single!.IsInMultiUserHost ? single : null;
    }

    public static IDisposable PushAmbient(BevToolsSession session)
    {
        var previous = Ambient.Value;
        Ambient.Value = session;
        return new AmbientScope(previous);
    }

    private static void Prune()
    {
        Sessions.RemoveAll(w => !w.TryGetTarget(out var s) || s.IsDisposed);
        var list = new BevToolsSession?[Sessions.Count];
        for (var i = 0; i < Sessions.Count; i++)
        {
            Sessions[i].TryGetTarget(out list[i]);
        }

        // Read as sessions come and go, not per lookup. A session's host can only turn out to serve many users later (its
        // platform reported), never the reverse, so a stale false only costs the scan it would have saved.
        Volatile.Write(ref _allInMultiUserHost, list.Length > 0 && Array.TrueForAll(list, s => s is null or { IsInMultiUserHost: true }));
        Volatile.Write(ref _cache, list);
    }

    private sealed class AmbientScope(BevToolsSession? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }

    private sealed class OwnWorkScope(bool previous) : IDisposable
    {
        public void Dispose() => OwnWork.Value = previous;
    }
}
