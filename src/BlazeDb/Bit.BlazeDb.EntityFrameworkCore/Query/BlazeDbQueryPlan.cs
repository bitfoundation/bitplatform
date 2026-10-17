using System.Linq.Expressions;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// A translated query in the terms the engine understands: an optional index source, residual
/// predicates, an ordering, and paging. The translator builds one without knowing the row type
/// statically, so every member here is non-generic; <see cref="BlazeDbQueryPlan{TRow}"/> supplies the
/// typed implementation.
/// </summary>
internal abstract class BlazeDbQueryPlan
{
    /// <summary>The index the plan reads from, or null for a full scan.</summary>
    public string? IndexName { get; protected set; }

    /// <summary>True once an index source is chosen; a plan has at most one.</summary>
    public bool HasIndexSource { get; protected set; }

    /// <summary>True when the index source already yields rows in the order the query asked for.</summary>
    public bool IndexProvidesOrder { get; protected set; }

    public bool HasOrdering { get; protected set; }

    public int Skip { get; set; }

    public int Take { get; set; } = -1;

    /// <summary>True when the plan reads straight from the primary-key dictionary (one key or a list of keys).</summary>
    public bool UsesPrimaryKey { get; protected set; }

    /// <summary>Sources the one row with the given primary key (already of the table's key type).</summary>
    public abstract void UseKey(object key);

    /// <summary>Sources the rows whose primary key is one of <paramref name="keys"/> (distinct, key-typed).</summary>
    public abstract void UseKeys(IReadOnlyList<object> keys);

    /// <summary>
    /// Sources the rows whose indexed value equals <paramref name="key"/>. A null key asks for the
    /// rows whose value is null; the caller has checked that the index can hold one.
    /// </summary>
    public abstract void UseEquality(BlazeDbIndex index, object? key);

    /// <summary>Sources the union of the index lookups for <paramref name="keys"/> (distinct, key-typed, null allowed as for <see cref="UseEquality"/>).</summary>
    public abstract void UseEqualityIn(BlazeDbIndex index, IReadOnlyList<object?> keys);

    public abstract void UseRange(BlazeDbIndex index, bool hasFrom, object? from, bool hasTo, object? to, bool descending);

    /// <summary>Changes the direction an already-chosen range is walked in.</summary>
    public abstract void SetRangeDirection(bool descending);

    /// <summary>Adds a residual filter from an <c>Expression&lt;Func&lt;TRow, bool&gt;&gt;</c>.</summary>
    public abstract void AddPredicate(LambdaExpression predicate);

    /// <summary>Adds an <c>OrderBy</c>/<c>ThenBy</c> step, taken straight from the LINQ call.</summary>
    public abstract void AddOrdering(MethodCallExpression call);
}
