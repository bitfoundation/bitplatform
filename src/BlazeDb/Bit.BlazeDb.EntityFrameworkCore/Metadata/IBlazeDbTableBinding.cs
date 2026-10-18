using Bit.BlazeDb.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

/// <summary>
/// The non-generic face of one mapped table, so the provider can work with entity types without
/// knowing their key or row type statically.
/// </summary>
internal interface IBlazeDbTableBinding
{
    Type RowType { get; }

    string TableName { get; }

    /// <summary>The CLR type of the primary key.</summary>
    Type KeyType { get; }

    /// <summary>
    /// The entity property that holds the primary key, when EF's model names exactly one - which
    /// lets the translator send <c>row.Id == value</c> to the key dictionary. Null when the model's
    /// key does not map onto a single property.
    /// </summary>
    string? KeyMember { get; }

    IReadOnlyList<BlazeDbIndex> Indexes { get; }

    /// <summary>Creates an empty plan typed to this table's row type.</summary>
    BlazeDbQueryPlan CreatePlan();

    /// <summary>Runs a translated plan and returns the live row objects it selects.</summary>
    IEnumerable<object> Execute(BlazeDbQueryPlan plan);

    /// <summary>Wraps the plan's rows in a queryable, for the LINQ the plan could not absorb.</summary>
    IQueryable AsQueryable(IEnumerable<object> rows);

    void Insert(object row);

    /// <summary>
    /// Applies an update where <paramref name="row"/> is normally the instance the table already
    /// holds, mutated in place. <paramref name="previousValues"/> carries the values the indexes
    /// were built from, without which stale index entries would be left behind. A detached
    /// <paramref name="row"/> - attached by key, so its original values are only its own - replaces
    /// the held instance instead.
    /// </summary>
    void UpdateInPlace(object row, object previousValues);

    /// <summary>
    /// Deletes a row, using <paramref name="previousValues"/> to retract index entries. Throws
    /// <see cref="KeyNotFoundException"/> when the row is already gone.
    /// </summary>
    void Delete(object row, object previousValues);
}
