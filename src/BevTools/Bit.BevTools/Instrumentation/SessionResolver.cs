
namespace Bit.BevTools;

/// <summary>
/// Maps ambient execution context to a BevTools session.
/// <para>
/// Resolution order: (1) an explicit ambient session pushed by <see cref="BevToolsSession.UseAmbient"/>;
/// (2) the session whose renderer synchronization context is current — an O(1) lookup, which is the case for
/// everything that runs on a Blazor dispatcher; (3) a scan comparing <c>Dispatcher.CheckAccess()</c>;
/// (4) for single-session hosts that are not Blazor Server (WebAssembly, WebView, tests) the only live session.
/// </para>
/// <para>
/// The last fallback deliberately excludes Blazor Server: attributing ambient work to "the only circuit" would
/// mis-attribute background/server activity to whichever user happened to be connected.
/// </para>
/// </summary>
internal static class SessionResolver
{
    private static readonly object Lock = new();
    private static readonly List<WeakReference<BevToolsSession>> Sessions = [];
    private static readonly AsyncLocal<BevToolsSession?> Ambient = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SynchronizationContext, WeakReference<BevToolsSession>> ByContext = new();
    private static BevToolsSession?[] _cache = [];

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

    public static BevToolsSession? Resolve()
    {
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

        // Single-session hosts (WebAssembly, WebView, tests) have no other candidate. Blazor Server is excluded on
        // purpose: "the only circuit right now" is not evidence that this activity belongs to that user.
        return liveCount == 1 && !string.Equals(single!.Platform, "Server", StringComparison.Ordinal) ? single : null;
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

        Volatile.Write(ref _cache, list);
    }

    private sealed class AmbientScope(BevToolsSession? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }
}
