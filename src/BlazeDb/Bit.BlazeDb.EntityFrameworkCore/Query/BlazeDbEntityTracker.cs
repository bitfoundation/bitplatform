using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Puts a row a query returned into the change tracker, with the identity resolution every EF
/// provider performs: when the context already tracks an entity with that row's key, that entity
/// is what the query hands back, and the row is left alone.
/// <para>
/// Normally the two are the same object, since queries return the table's live rows. They part
/// ways when a save is rolled back - the table goes back to holding the values it had, as a
/// separate instance, while the tracker keeps the object whose save was undone. Tracking the
/// table's instance then would be an identity conflict, and the tracker's instance is the one the
/// application is holding and will retry with, which is what makes it the right answer.
/// </para>
/// </summary>
internal static class BlazeDbEntityTracker
{
    private static readonly ConcurrentDictionary<Type, Func<DbContext, object?[], object?>> Finders = new();

    private static readonly MethodInfo FindMethod =
        typeof(BlazeDbEntityTracker).GetMethod(nameof(Find), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static object Track(object row, IEntityType entityType, DbContext context)
    {
        if (entityType.FindPrimaryKey() is { } key)
        {
            var values = new object?[key.Properties.Count];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = key.Properties[i].GetGetter().GetClrValue(row);
            }
            var finder = Finders.GetOrAdd(entityType.ClrType, static type =>
                (Func<DbContext, object?[], object?>)FindMethod
                    .MakeGenericMethod(type)
                    .CreateDelegate(typeof(Func<DbContext, object?[], object?>)));
            if (finder(context, values) is { } tracked)
            {
                return tracked;
            }
        }

        // Unchanged, not Added: the row is already in the table, and this snapshots its current
        // values as the originals a later SaveChanges compares against.
        var entry = context.Entry(row);
        if (entry.State == EntityState.Detached)
        {
            entry.State = EntityState.Unchanged;
        }
        return row;
    }

    private static object? Find<TEntity>(DbContext context, object?[] keyValues)
        where TEntity : class =>
        context.Set<TEntity>().Local.FindEntryUntyped(keyValues)?.Entity;
}
