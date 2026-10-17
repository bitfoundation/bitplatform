using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.EntityFrameworkCore.Infrastructure;

/// <summary>
/// The contexts currently attached to one engine.
/// <para>
/// Queries hand back the table's own row objects, so a tracked entity that one context has
/// modified but not yet saved has already changed the row every other context sees - while the
/// table's secondary indexes still describe the values it had. A context can check its own change
/// tracker before trusting an index; it cannot see another context's. This registry is how it
/// asks: every context registers on first use, and an indexed query consults the other live
/// contexts' trackers for pending modifications to the entity type it is about to read.
/// </para>
/// <para>
/// Contexts are held weakly and pruned when collected or disposed, so registering costs nothing
/// in lifetime. The registry is keyed on the engine, not the store name, so contexts over an
/// engine the application opened itself take part as well.
/// </para>
/// </summary>
internal sealed class BlazeDbContextRegistry
{
    private static readonly ConditionalWeakTable<BlazeDbDatabase, BlazeDbContextRegistry> Registries = new();

    private readonly object _gate = new();
    private readonly List<WeakReference<DbContext>> _contexts = [];

    public static BlazeDbContextRegistry For(BlazeDbDatabase database) => Registries.GetOrCreateValue(database);

    public void Register(DbContext context)
    {
        lock (_gate)
        {
            for (var i = _contexts.Count - 1; i >= 0; i--)
            {
                if (!_contexts[i].TryGetTarget(out var existing))
                {
                    _contexts.RemoveAt(i);
                }
                else if (ReferenceEquals(existing, context))
                {
                    return;
                }
            }
            _contexts.Add(new WeakReference<DbContext>(context));
        }
    }

    public void Unregister(DbContext context)
    {
        lock (_gate)
        {
            _contexts.RemoveAll(w => !w.TryGetTarget(out var target) || ReferenceEquals(target, context));
        }
    }

    /// <summary>
    /// Whether a context other than <paramref name="asking"/> holds a modified, unsaved entity of
    /// <paramref name="clrType"/>. Reads the other trackers as they are, which runs their change
    /// detection under the same single-threaded assumption the engine itself makes.
    /// </summary>
    public bool OthersHaveModified(Type clrType, DbContext asking)
    {
        DbContext[] others;
        lock (_gate)
        {
            if (_contexts.Count <= 1)
            {
                return false;
            }
            others = new DbContext[_contexts.Count];
            var n = 0;
            foreach (var weak in _contexts)
            {
                if (weak.TryGetTarget(out var context) && !ReferenceEquals(context, asking))
                {
                    others[n++] = context;
                }
            }
            if (n == 0)
            {
                return false;
            }
            Array.Resize(ref others, n);
        }

        foreach (var context in others)
        {
            try
            {
                foreach (var entry in context.ChangeTracker.Entries())
                {
                    if (entry.State == EntityState.Modified && clrType.IsInstanceOfType(entry.Entity))
                    {
                        return true;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Unregister(context);
            }
        }
        return false;
    }
}
