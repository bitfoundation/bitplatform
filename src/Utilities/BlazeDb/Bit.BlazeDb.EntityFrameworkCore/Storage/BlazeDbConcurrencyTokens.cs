using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// Checks the properties a model marks as concurrency tokens - <c>[ConcurrencyCheck]</c>,
/// <c>IsConcurrencyToken()</c>, <c>IsRowVersion()</c> - the way a relational provider's
/// <c>WHERE Token = @original</c> does: an update or delete whose original token values are not the
/// ones the row was last committed with fails with <see cref="DbUpdateConcurrencyException"/>.
/// <para>
/// Rows are live objects shared by every context on the engine, so the row itself cannot say what
/// was committed: another context may have changed it in memory without saving. The token values are
/// therefore recorded at the moment the table is given a row - loaded or replayed from storage, written
/// by a save or through the engine's own API, put back by a rollback - against the row instance and the
/// engine's sequence number for that write (see <see cref="Observe"/>). The table reports every row it
/// holds when the provider first binds to it, so a row read from storage has its record before any
/// query of the provider hands it to a context. Any later write gives the row a new number, and with it
/// a new record; a rollback puts the old number back, and the record kept for it applies again.
/// </para>
/// <para>
/// The cost is one record per row of a table whose entity type has tokens, for as long as the row is
/// held. Types without tokens record nothing.
/// </para>
/// </summary>
internal static class BlazeDbConcurrencyTokens
{
    private static readonly ConditionalWeakTable<IEntityType, IProperty[]> Tokens = new();

    private static readonly ConditionalWeakTable<object, Snapshot> Committed = new();

    /// <summary>
    /// The values of <see cref="Properties"/> that the write numbered <see cref="Seq"/> committed, and the
    /// record it replaced, which applies again should that write be rolled back. The properties are kept
    /// with the values: a context of another model, with other tokens, may share the engine and its rows.
    /// </summary>
    private sealed record Snapshot(IProperty[] Properties, object?[] Values, long Seq, Snapshot? Previous);

    /// <summary>The entity type's concurrency tokens; empty for most types.</summary>
    public static IProperty[] Of(IEntityType entityType) =>
        Tokens.GetValue(entityType, static type => [.. type.GetProperties().Where(p => p.IsConcurrencyToken)]);

    /// <summary>
    /// Refuses a concurrency token that is a shadow property. A row is the CLR object alone - the engine
    /// stores no shadow values - so such a token would have nothing to be stored in or checked against.
    /// </summary>
    public static void ThrowIfShadowToken(IEntityType entityType)
    {
        foreach (var property in entityType.GetDeclaredProperties())
        {
            if (property.IsConcurrencyToken && property.IsShadowProperty())
            {
                throw new NotSupportedException(
                    $"The concurrency token '{property.Name}' of '{entityType.DisplayName()}' is a shadow property. " +
                    "BlazeDb stores a row as its CLR object and keeps no shadow values, so the token could be " +
                    "neither stored nor checked. Map it to a property or field of the class.");
            }
        }
    }

    /// <summary>
    /// Throws when <paramref name="entry"/>'s original token values are not the ones
    /// <paramref name="held"/> - the row the table holds under its key, written by the write numbered
    /// <paramref name="heldSeq"/> - was committed with.
    /// </summary>
    public static void Check(IUpdateEntry entry, IProperty[] tokens, object held, long heldSeq)
    {
        var snapshot = Committed.TryGetValue(held, out var current) ? Find(current, heldSeq) : null;
        var isEntity = ReferenceEquals(held, entry.ToEntityEntry().Entity);

        for (var i = 0; i < tokens.Length; i++)
        {
            object? committed;
            if (snapshot is not null && IndexOf(snapshot, tokens[i]) is var at and >= 0)
            {
                committed = snapshot.Values[at];
            }
            else if (isEntity)
            {
                // No record of this token: the table was first bound by a context of another model, which does
                // not check it. The held row's own value is all there is then, and for the very instance being
                // saved that is this context's change rather than what was committed, so nothing can be told.
                continue;
            }
            else
            {
                committed = tokens[i].GetGetter().GetClrValueUsingContainingEntity(held);
            }

            if (!tokens[i].GetValueComparer().Equals(entry.GetOriginalValue(tokens[i]), committed))
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
    /// Records the token values <paramref name="row"/> is being committed with by the write numbered
    /// <paramref name="seq"/>. Called by the table under its write lock, as the row is set. A row put back
    /// under a number it already has a record for - a rollback - keeps that record, made when the values
    /// were committed rather than now, when a context may have changed them in memory since.
    /// </summary>
    public static void Observe(object row, IProperty[] tokens, long seq)
    {
        try
        {
            Committed.TryGetValue(row, out var current);
            if (current is not null && Find(current, seq) is not null)
            {
                return;
            }
            // One level back is enough to undo the write that is running; older records are dropped, so a row
            // written over and over does not drag a chain of them along.
            Committed.AddOrUpdate(row, new Snapshot(tokens, Read(row, tokens), seq, current is null ? null : current with { Previous = null }));
        }
        catch
        {
            // Called in the middle of the engine's write, which must not fail for it: a getter that throws leaves
            // the row without a record, and its check falls back to what the row holds.
            Committed.Remove(row);
        }
    }

    /// <summary>
    /// Where <paramref name="snapshot"/> keeps <paramref name="token"/>'s value, or -1 when it does not. A record
    /// made through another model's tokens is read by name and type; values are never matched by position alone.
    /// </summary>
    private static int IndexOf(Snapshot snapshot, IProperty token)
    {
        var properties = snapshot.Properties;
        for (var i = 0; i < properties.Length; i++)
        {
            if (ReferenceEquals(properties[i], token) || (properties[i].Name == token.Name && properties[i].ClrType == token.ClrType))
            {
                return i;
            }
        }
        return -1;
    }

    // A record for another write describes values the row has moved on from - written since, or rolled back -
    // and is not trusted.
    private static Snapshot? Find(Snapshot snapshot, long seq) =>
        snapshot.Seq == seq ? snapshot
        : snapshot.Previous is { } previous && previous.Seq == seq ? previous
        : null;

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
