using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.BevTools;

/// <summary>
/// <see cref="BevToolsAutoStart.Logs"/>: decides whose entries are kept before BevTools is attached, keeps them, and
/// hands them to the session that attaches. A process that serves one user (Blazor WebAssembly, Blazor Hybrid) keeps
/// one buffer, which the first session attached in the process takes. A process that serves many users keeps one per
/// circuit, of the entries written on its renderer's synchronization context - the evidence Bit.BevTools' own session
/// resolver goes by first - all of them within one budget for the process, and none where no attach could ever be
/// allowed. Singleton, like the loader's context.
/// </summary>
internal sealed class BevToolsAutoStartLogs(BevToolsLoaderContext context)
{
    /// <summary>All circuits' buffers together hold at most this many times <see cref="BevToolsLoaderOptions.MaxLogs"/>.</summary>
    internal const int CircuitsBudgetFactor = 10;

    private const int Unresolved = 0;
    private const int Off = 1;
    private const int Process = 2;
    private const int PerScope = 3;

    private readonly ConditionalWeakTable<SynchronizationContext, BevToolsAutoStartLogBuffer> _scopes = new();
    private int _mode;
    private bool _readsValues;
    private int _held;
    private BevToolsAutoStartLogBuffer? _process;
    private string _reason = "";

    public BevToolsLoaderContext Context { get; } = context;

    /// <summary>
    /// Decides once, from the first service provider that asks - the logger factory's, or a scope's activator hook -
    /// which needs the container: whether this process serves many users, whether an attach is allowed there, and
    /// whether the session that attaches would read values at all. A failure decides "off": it must not stop the app's
    /// logger factory, which asks first, from being built.
    /// </summary>
    public void Resolve(IServiceProvider services)
    {
        if (Volatile.Read(ref _mode) != Unresolved)
        {
            return;
        }

        int mode;
        string reason;
        var readsValues = false;
        BevToolsAutoStartLogBuffer? process = null;
        try
        {
            if (!Context.IsMultiUserHost)
            {
                (mode, reason, readsValues) = (Process, "in this process, which serves one user", true);
                process = new BevToolsAutoStartLogBuffer(Context.Options.MaxLogs, "in this process");
            }
            else if (Context.ResolveAttach(services) is { Allowed: true })
            {
                // Outside Development a session attached here reads no values (BevToolsOptions.ValueAccess is None by
                // default), so neither does the capture: no formatter, no ToString, no message is run or kept for it.
                readsValues = services.GetService<IHostEnvironment>()?.IsDevelopment() == true;
                (mode, reason) = (PerScope, "per circuit, of what was written on its dispatcher" + (readsValues ? "" : ", as names and types only, since a session attached outside Development reads no values"));
            }
            else
            {
                // Nobody could ever attach, so nobody could ever read them: keeping them would be memory spent on nothing.
                (mode, reason) = (Off, "this process serves many users and BevTools may not be attached here, so nothing was kept for an attach that cannot happen");
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            (mode, reason) = (Off, "deciding where to keep them failed (" + ex.GetType().Name + ": " + ex.Message + ")");
        }

        lock (_scopes)
        {
            if (_mode != Unresolved)
            {
                return;
            }

            _process = process;
            _reason = reason;
            _readsValues = readsValues;
            Volatile.Write(ref _mode, mode);
        }
    }

    /// <summary>Where entries are kept (or why they are not), as the end of a sentence; empty until resolved.</summary>
    public string Reason => Volatile.Read(ref _mode) == Unresolved ? "" : _reason;

    /// <summary>Whether an entry's values, message and scopes are read as text, or kept as names and types only.</summary>
    public bool ReadsValues => Volatile.Read(ref _mode) != Unresolved && _readsValues;

    /// <summary>True where entries are kept per circuit, which a session takes over once it knows its circuit's dispatcher.</summary>
    public bool KeepsPerScope(IServiceProvider services)
    {
        Resolve(services);
        return Volatile.Read(ref _mode) == PerScope;
    }

    /// <summary>
    /// False once nothing more can be kept, so the logger refuses an entry before it is formatted: nothing is kept at
    /// all, or the one buffer of a single-user process was taken.
    /// </summary>
    public bool IsCapturing => Volatile.Read(ref _mode) switch
    {
        Process => _process is { IsTaken: false },
        PerScope => true,
        _ => false,
    };

    /// <summary>The buffer an entry logged here and now belongs to, or null when it belongs to nobody who could attach.</summary>
    public BevToolsAutoStartLogBuffer? Current()
    {
        switch (Volatile.Read(ref _mode))
        {
            case Process:
                return _process is { IsTaken: false } process ? process : null;
            case PerScope:
                // Not a guess: a circuit's components run on its renderer's synchronization context, and nothing else does.
                // Work that left it (an await with ConfigureAwait(false)) has none, and is not kept.
                return SynchronizationContext.Current is { } current && _scopes.TryGetValue(current, out var scoped) && !scoped.IsTaken ? scoped : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// Called by the scope's activator hook when it creates its first component, which is on the renderer's
    /// synchronization context: from then on what is logged on that context is the scope's. Null where entries are not
    /// kept per scope, with <paramref name="whyNone"/> saying why when they are kept per scope but not for this one.
    /// </summary>
    public BevToolsAutoStartLogBuffer? NoteScope(IServiceProvider scopeServices, out string? whyNone)
    {
        whyNone = null;
        Resolve(scopeServices);
        if (Volatile.Read(ref _mode) != PerScope)
        {
            return null;
        }

        if (SynchronizationContext.Current is not { } current)
        {
            whyNone = "this scope created its first component without a synchronization context, so what it logged could not be told apart from other circuits'";
            return null;
        }

        return _scopes.GetValue(current, _ => new BevToolsAutoStartLogBuffer(Context.Options.MaxLogs, "on this circuit's dispatcher", this));
    }

    /// <summary>Takes one entry's place in the budget all circuits share; false when it is spent.</summary>
    internal bool TryReserve()
    {
        var budget = (long)Math.Max(1, Context.Options.MaxLogs) * CircuitsBudgetFactor;
        while (true)
        {
            var held = Volatile.Read(ref _held);
            if (held >= budget)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _held, held + 1, held) == held)
            {
                return true;
            }
        }
    }

    /// <summary>Gives back what a circuit's buffer held, once it was handed over or its scope ended.</summary>
    internal void Release(int entries) => Interlocked.Add(ref _held, -entries);

    /// <summary>
    /// Hands the scope's entries to the session attached in it, and stops keeping them: from now on the session records
    /// on its own. Null, with <paramref name="whyNone"/> saying why, when there is nothing to hand over.
    /// </summary>
    public BevToolsAutoStartLogEntry[]? Take(IServiceProvider scopeServices, out BevToolsAutoStartLogBuffer? buffer, out int[] counts, out string whyNone)
    {
        Resolve(scopeServices);
        counts = [];
        buffer = null;
        BevToolsActivatorHook? hook = null;
        switch (Volatile.Read(ref _mode))
        {
            case Process:
                buffer = _process;
                break;
            case PerScope:
                hook = scopeServices.GetService(typeof(BevToolsActivatorHook)) as BevToolsActivatorHook;
                buffer = hook?.AutoStartLogs;
                break;
        }

        if (buffer is null)
        {
            whyNone = Volatile.Read(ref _mode) != PerScope
                ? _reason
                : hook?.AutoStartLogsGap
                    ?? "this circuit's components were not created through Bit.BevTools.Loader's activator hook (an IComponentActivator registered after AddBitBevToolsLoaderServices replaced it), so what was logged on its dispatcher could not be told apart from other circuits'";
            return null;
        }

        if (buffer.Take(out counts) is not { } entries)
        {
            whyNone = "they were handed to the first session attached " + buffer.Owner;
            return null;
        }

        whyNone = "";
        return entries;
    }
}
