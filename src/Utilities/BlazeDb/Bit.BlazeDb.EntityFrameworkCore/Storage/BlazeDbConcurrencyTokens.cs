using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// Checks the properties a model marks as concurrency tokens - <c>[ConcurrencyCheck]</c>,
/// <c>IsConcurrencyToken()</c>, <c>IsRowVersion()</c> - the way a relational provider's
/// <c>WHERE Token = @original</c> does: an update or delete whose original token values are not the
/// ones the row was last saved with fails with <see cref="DbUpdateConcurrencyException"/>.
/// <para>
/// Rows are live objects shared by every context on the engine, so the row itself cannot say what
/// was last saved: another context may have changed it in memory without saving. The token values
/// each save commits are therefore kept here, against the row instance the table holds. A row no
/// save in this process has written yet - one read from storage - has its committed values on
/// itself, unless it is the very instance being saved, whose values this context has just changed:
/// then nothing tells what is committed, and the save goes ahead.
/// </para>
/// </summary>
internal static class BlazeDbConcurrencyTokens
{
    private static readonly ConditionalWeakTable<IEntityType, IProperty[]> Tokens = new();

    private static readonly ConditionalWeakTable<object, object?[]> Committed = new();

    /// <summary>The entity type's concurrency tokens; empty for most types.</summary>
    public static IProperty[] Of(IEntityType entityType) =>
        Tokens.GetValue(entityType, static type => [.. type.GetProperties().Where(p => p.IsConcurrencyToken)]);

    /// <summary>
    /// Throws when <paramref name="entry"/>'s original token values are not the ones
    /// <paramref name="held"/> - the row the table holds under its key - was last saved with.
    /// </summary>
    public static void Check(IUpdateEntry entry, IProperty[] tokens, object held)
    {
        object?[]? committed;
        if (!Committed.TryGetValue(held, out committed))
        {
            if (ReferenceEquals(held, entry.ToEntityEntry().Entity))
            {
                return;
            }
            committed = Read(held, tokens);
        }

        for (var i = 0; i < tokens.Length; i++)
        {
            if (!tokens[i].GetValueComparer().Equals(entry.GetOriginalValue(tokens[i]), committed[i]))
            {
                throw new DbUpdateConcurrencyException(
                    $"The concurrency token '{tokens[i].Name}' of this entity no longer has the value it was " +
                    "loaded with: the row was saved by another context, or replaced by key, after this one " +
                    "read it. Reload the entity and apply the change again.",
                    [entry]);
            }
        }
    }

    /// <summary>Remembers the token values a committed save left <paramref name="row"/> with.</summary>
    public static void Record(object row, IProperty[] tokens) => Committed.AddOrUpdate(row, Read(row, tokens));

    private static object?[] Read(object row, IProperty[] tokens)
    {
        var values = new object?[tokens.Length];
        for (var i = 0; i < tokens.Length; i++)
        {
            // A snapshot rather than the live value: a byte[] row version changed in place must not
            // change what was committed along with it.
            values[i] = tokens[i].GetValueComparer().Snapshot(tokens[i].GetGetter().GetClrValueUsingContainingEntity(row));
        }
        return values;
    }
}
