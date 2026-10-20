using System.Linq.Expressions;
using System.Reflection;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Bit.BlazeDb.Querying;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

internal sealed class BlazeDbQueryPlan<TRow> : BlazeDbQueryPlan
{
    private object? _primaryKey;
    private IReadOnlyList<object>? _primaryKeys;
    private BlazeDbIndex? _index;
    private bool _hasEqualityKey;
    private object? _equalityKey;
    private IReadOnlyList<object?>? _equalityKeys;
    private bool _hasFrom;
    private object? _from;
    private bool _hasTo;
    private object? _to;
    private bool _descending;
    private List<Func<TRow, bool>>? _predicates;
    private Func<IEnumerable<TRow>, IOrderedEnumerable<TRow>>? _order;

    public override void UseKey(object key)
    {
        _primaryKey = key;
        HasIndexSource = true;
        UsesPrimaryKey = true;
        IndexName = null;
    }

    public override void UseKeys(IReadOnlyList<object> keys)
    {
        _primaryKeys = keys;
        HasIndexSource = true;
        UsesPrimaryKey = true;
        IndexName = null;
    }

    public override void UseEquality(BlazeDbIndex index, object? key)
    {
        _index = index;
        _hasEqualityKey = true;
        _equalityKey = key;
        HasIndexSource = true;
        IndexName = index.Name;
    }

    public override void UseEqualityIn(BlazeDbIndex index, IReadOnlyList<object?> keys)
    {
        _index = index;
        _equalityKeys = keys;
        HasIndexSource = true;
        IndexName = index.Name;
    }

    public override void UseRange(BlazeDbIndex index, bool hasFrom, object? from, bool hasTo, object? to, bool descending)
    {
        _index = index;
        _hasFrom = hasFrom;
        _from = from;
        _hasTo = hasTo;
        _to = to;
        _descending = descending;
        HasIndexSource = true;
        IndexProvidesOrder = true;
        IndexName = index.Name;
    }

    public override void SetRangeDirection(bool descending)
    {
        if (!IndexProvidesOrder)
        {
            throw new InvalidOperationException("The plan has no range source to redirect.");
        }
        _descending = descending;
    }

    public override void AddPredicate(LambdaExpression predicate) =>
        (_predicates ??= []).Add((Func<TRow, bool>)BlazeDbDelegateCache.Compile(predicate));

    public override void AddOrdering(MethodCallExpression call)
    {
        // Reuse the LINQ-to-Objects operator that matches the Queryable one, with the same generic
        // arguments, rather than reconstructing the comparison ourselves - that keeps null and
        // comparer semantics identical to any other LINQ provider.
        var selector = BlazeDbDelegateCache.Compile((LambdaExpression)StripQuotes(call.Arguments[1]));
        var method = BlazeDbDelegateCache.EnumerableOperator(call.Method.Name, call.Method.GetGenericArguments());
        var previous = _order;

        _order = previous is null
            ? rows => (IOrderedEnumerable<TRow>)method.Invoke(null, [rows, selector])!
            : rows => (IOrderedEnumerable<TRow>)method.Invoke(null, [previous(rows), selector])!;
        HasOrdering = true;
    }

    /// <summary>Composes this plan onto an engine query over the table it was translated against.</summary>
    public BlazeDbQuery<TKey, TRow> Build<TKey>(BlazeDbQuery<TKey, TRow> query)
        where TKey : notnull
    {
        if (_primaryKey is not null)
        {
            query = query.UseKey((TKey)_primaryKey);
        }
        else if (_primaryKeys is not null)
        {
            query = query.UseKeys(_primaryKeys.Cast<TKey>());
        }
        else if (_index is not null)
        {
            var definition = (BlazeDbIndexDefinition<TRow>)_index.Definition;
            query = _hasEqualityKey ? query.UseIndexBoxed(definition, _equalityKey)
                : _equalityKeys is not null ? query.UseIndexBoxed(definition, _equalityKeys)
                : query.UseIndexBoxed(definition, _hasFrom, _from, _hasTo, _to, _descending);
        }
        if (_predicates is not null)
        {
            foreach (var predicate in _predicates)
            {
                query.Where(predicate);
            }
        }
        if (_order is not null)
        {
            query.OrderBy(_order);
        }
        if (Skip > 0)
        {
            query.Skip(Skip);
        }
        if (Take >= 0)
        {
            query.Take(Take);
        }
        return query;
    }

    private static Expression StripQuotes(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Quote } quote)
        {
            expression = quote.Operand;
        }
        return expression;
    }
}
