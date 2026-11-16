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
/// <para>
/// What a save records is tied to the engine's sequence number for the write that made it. Any later
/// write to the row - one through the engine's own API included - gives the row a new number, so the
/// record no longer applies and the row's own values are read instead; a rollback puts the old number
/// back, and with it the record from before the save that was rolled back. Recording happens inside
/// the save's transaction, under the engine's write lock, so no other save can check against the
/// values from before it once its write has landed.
/// </para>
/// </summary>
internal static class BlazeDbConcurrencyTokens
{
    private static readonly ConditionalWeakTable<IEntityType, IProperty[]> Tokens = new();

    private static readonly ConditionalWeakTable<object, Snapshot> Committed = new();

    /// <summary>
    /// The token values the write numbered <see cref="Seq"/> committed, and the record it replaced,
    /// which applies again should that write be rolled back.
    /// </summary>
    private sealed record Snapshot(object?[] Values, long Seq, Snapshot? Previous);

    /// <summary>The entity type's concurrency tokens; empty for most types.</summary>
    public static IProperty[] Of(IEntityType entityType) =>
        Tokens.GetValue(entityType, static type => [.. type.GetProperties().Where(p => p.IsConcurrencyToken)]);

    /// <summary>
    /// Throws when <paramref name="entry"/>'s original token values are not the ones
    /// <paramref name="held"/> - the row the table holds under its key, written by the write numbered
    /// <paramref name="heldSeq"/> - was last saved with.
    /// </summary>
    public static void Check(IUpdateEntry entry, IProperty[] tokens, object held, long heldSeq)
    {
        var committed = CommittedAt(held, heldSeq);
        if (committed is null)
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

    /// <summary>
    /// Remembers the token values the write numbered <paramref name="seq"/> left <paramref name="row"/>
    /// with. <paramref name="previousSeq"/> is the number the row had before that write, when it had one:
    /// the record for it is kept, should the write be rolled back.
    /// </summary>
    public static void Record(object row, IProperty[] tokens, long seq, long? previousSeq)
    {
        // One level back is enough to undo the save that is running; older records are dropped, so a row
        // saved over and over does not drag a chain of them along.
        var previous = previousSeq is { } before && FindAt(row, before) is { } found ? found with { Previous = null } : null;
        Committed.AddOrUpdate(row, new Snapshot(Read(row, tokens), seq, previous));
    }

    private static object?[]? CommittedAt(object row, long seq) => FindAt(row, seq)?.Values;

    private static Snapshot? FindAt(object row, long seq)
    {
        if (!Committed.TryGetValue(row, out var snapshot))
        {
            return null;
        }
        // A record for another write describes values the row has moved on from - written since through
        // the engine's API, or rolled back - and is not trusted.
        return snapshot.Seq == seq ? snapshot
            : snapshot.Previous is { } previous && previous.Seq == seq ? previous
            : null;
    }

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
