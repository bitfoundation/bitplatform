using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Runs a LINQ query against the engine.
///
/// A query is translated as far as the engine's plan model reaches - index source, filters,
/// ordering, paging - and whatever is left runs as LINQ to Objects over the rows that come back.
/// Nothing is materialized on the way: the objects the query yields are the ones the table holds,
/// so a tracked entity mutated afterwards has already changed the database in memory, and
/// <c>SaveChanges</c> is what commits that change to the log.
/// </summary>
internal sealed class BlazeDbQueryExecutor
{
    private readonly IBlazeDbTableCache _tables;

    public BlazeDbQueryExecutor(IBlazeDbTableCache tables) => _tables = tables;

    public object? Execute(Expression query, DbContext context)
    {
        query = BlazeDbQueryPreparer.Prepare(query, out var trackingOverride);

        var root = BlazeDbQueryPreparer.FindRoot(query);
        var entityType = root.EntityType;
        var binding = _tables.GetBinding(entityType);

        // Any other set the query mentions - the inner side of a join, a subquery inside a Where -
        // is bound to its table's rows here, so the rest of the pipeline only ever sees one query
        // root. Those rows are a scan: the plan is chosen for the root set alone, and the inner
        // ones run as LINQ to Objects, which is what they would be left to anyway.
        query = BlazeDbQueryRootBinder.BindOtherSets(query, BindSet);

        var translated = BlazeDbQueryTranslator.Translate(query, binding, entityType.ClrType);
        if (translated.Plan.HasIndexSource && !translated.Plan.UsesPrimaryKey && HasUnsavedChangesTo(entityType, context))
        {
            // A tracked entity that has been modified but not saved has already changed the row in
            // memory - the provider's contract - but its index entries still describe the old
            // values until SaveChanges moves them. A lookup through those entries would then miss
            // the row under its new value and return it under the one it no longer has, while a scan
            // sees the object as it is. So while such changes are pending - in this context or in
            // any other context on the same engine, since they all share the rows - the query reads
            // the rows themselves; the predicates stay as filters and the ordering is done by
            // sorting. (The primary key cannot be changed in place, so a key lookup stays exact.)
            translated = BlazeDbQueryTranslator.Translate(query, binding, entityType.ClrType, useIndexes: false);
        }

        var rows = binding.Execute(translated.Plan);
        var track = ShouldTrack(query, entityType, context, trackingOverride);

        var queryable = binding.AsQueryable(rows);
        if (translated.Remainder is null)
        {
            return track ? binding.AsQueryable(Track(rows, entityType, context)) : queryable;
        }

        var source = Expression.Constant(queryable, typeof(IQueryable<>).MakeGenericType(binding.RowType));
        var remainder = new BlazeDbRowsPlaceholderReplacer(source).Visit(translated.Remainder)!;

        // Tracking wraps what comes out of the remainder, not what goes into it: Single(predicate)
        // or Last walk every row the plan produced to find the one they return, and only that one
        // belongs in the change tracker - the way any other provider behaves.
        if (typeof(IQueryable).IsAssignableFrom(remainder.Type))
        {
            // A remainder that still describes a sequence has to be handed back as a query rather
            // than run: LINQ to Objects only evaluates the operators that reduce one to a value.
            var result = queryable.Provider.CreateQuery(remainder);
            return track ? binding.AsQueryable(Track((IEnumerable<object>)result, entityType, context)) : result;
        }

        var value = queryable.Provider.Execute(remainder);
        if (track && value is not null && entityType.ClrType.IsInstanceOfType(value))
        {
            value = BlazeDbEntityTracker.Track(value, entityType, context);
        }
        return value;
    }

    /// <summary>A queryable over every row of another set's table, read when it is enumerated.</summary>
    private Expression BindSet(EntityQueryRootExpression root)
    {
        var binding = _tables.GetBinding(root.EntityType);
        return Expression.Constant(binding.AsQueryable(binding.Execute(binding.CreatePlan())), root.Type);
    }

    /// <summary>
    /// Whether the context holds a modified, unsaved entity of this type - the state in which the
    /// table's secondary indexes lag the rows and cannot be trusted to answer a query about them.
    /// Only consulted once a plan has actually chosen an index, so the detect-changes pass it costs
    /// is paid for the queries that need it.
    /// </summary>
    private bool HasUnsavedChangesTo(IEntityType entityType, DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified && entityType.ClrType.IsInstanceOfType(entry.Entity))
            {
                return true;
            }
        }
        return _tables.OtherContextsHaveModified(entityType.ClrType);
    }

    /// <summary>
    /// Streams the same result, handing control back to the browser periodically. WebAssembly has
    /// one thread, so a long scan on it would freeze rendering until it finished.
    /// </summary>
    public async IAsyncEnumerable<T> ExecuteAsync<T>(
        Expression query, DbContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var result = Execute(query, context);
        if (result is not IEnumerable enumerable)
        {
            yield return (T)result!;
            yield break;
        }

        var sinceYield = 0;
        foreach (var item in enumerable)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return (T)item;
            if (++sinceYield == YieldEvery)
            {
                sinceYield = 0;
                await Task.Yield();
            }
        }
    }

    private const int YieldEvery = 512;

    /// <summary>
    /// Entities are tracked only when the query actually returns them. A projection, a count or an
    /// <c>Any</c> yields something else, and tracking the rows it happened to read would put
    /// entities in the change tracker the caller never asked for.
    /// </summary>
    private static bool ShouldTrack(Expression query, IEntityType entityType, DbContext context, bool? trackingOverride)
    {
        if (trackingOverride == false)
        {
            return false;
        }
        if (trackingOverride != true &&
            context.ChangeTracker.QueryTrackingBehavior != QueryTrackingBehavior.TrackAll)
        {
            return false;
        }
        return ResultElementType(query.Type) == entityType.ClrType;
    }

    private static Type ResultElementType(Type type)
    {
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(IQueryable<>) || definition == typeof(IEnumerable<>) ||
                definition == typeof(IOrderedQueryable<>) || definition == typeof(IAsyncEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }
        }
        return type;
    }

    /// <summary>
    /// Tracks each row as it goes by, yielding the instance the context tracks for that key - which
    /// is the row itself unless the context already holds another object with the same key (see
    /// <see cref="BlazeDbEntityTracker"/>).
    /// </summary>
    private static IEnumerable<object> Track(IEnumerable<object> rows, IEntityType entityType, DbContext context)
    {
        foreach (var row in rows)
        {
            yield return BlazeDbEntityTracker.Track(row, entityType, context);
        }
    }
}
