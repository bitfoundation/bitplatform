using System.Linq.Expressions;

namespace Bit.BlazorUI;

/// <summary>
/// Translates the grid's filter and sort descriptors into expression trees composed onto an
/// <see cref="IQueryable{T}"/>, so a remote LINQ provider (e.g. EF Core) executes them at the source
/// (SQL WHERE/ORDER BY/OFFSET) instead of the grid materializing the whole dataset into memory.
/// A descriptor that cannot be translated (unknown column, delegate-based sort key, or an operator
/// the member type does not support) is skipped rather than failing the query.
/// </summary>
public static class BitDataGridQueryableProcessor
{
    /// <summary>Applies the active filters then sorts onto the queryable (no paging).</summary>
    public static IQueryable<TItem> Apply<TItem>(
        IQueryable<TItem> source,
        IReadOnlyList<BitDataGridFilterDescriptor> filters,
        IReadOnlyList<BitDataGridSortDescriptor> sorts,
        IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns)
        => ApplySorts(ApplyFilters(source, filters, columns), sorts, columns);

    public static IQueryable<TItem> ApplyFilters<TItem>(
        IQueryable<TItem> source,
        IReadOnlyList<BitDataGridFilterDescriptor> filters,
        IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns)
    {
        foreach (var filter in filters)
        {
            if (!columns.TryGetValue(filter.ColumnId, out var column) || column.Accessor is null) continue;
            if (BuildPredicate<TItem>(column.Accessor, filter) is { } predicate)
            {
                source = source.Where(predicate);
            }
        }
        return source;
    }

    public static IQueryable<TItem> ApplySorts<TItem>(
        IQueryable<TItem> source,
        IReadOnlyList<BitDataGridSortDescriptor> sorts,
        IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns)
    {
        var active = sorts.Where(s => s.Direction != BitDataGridSortDirection.None).OrderBy(s => s.Priority);
        var first = true;
        foreach (var sort in active)
        {
            // Only field-backed columns translate; a SortBy delegate has no expression to compose.
            if (!columns.TryGetValue(sort.ColumnId, out var column) || column.Accessor is null) continue;

            var lambda = column.Accessor.PropertyLambda;
            var ascending = sort.Direction != BitDataGridSortDirection.Descending;
            var method = first
                ? (ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending))
                : (ascending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending));

            source = (IQueryable<TItem>)source.Provider.CreateQuery(Expression.Call(
                typeof(Queryable), method,
                new[] { typeof(TItem), lambda.Body.Type },
                source.Expression, Expression.Quote(lambda)));
            first = false;
        }
        return source;
    }

    /// <summary>
    /// Builds a translatable predicate for one filter descriptor, or <c>null</c> when the filter
    /// carries no criteria (empty value) or cannot be expressed for the member's type.
    /// </summary>
    private static Expression<Func<TItem, bool>>? BuildPredicate<TItem>(
        BitDataGridPropertyAccessor<TItem> accessor, BitDataGridFilterDescriptor filter)
    {
        var lambda = accessor.PropertyLambda;
        var param = lambda.Parameters[0];
        var member = lambda.Body;

        Expression? body = filter.Operator switch
        {
            BitDataGridFilterOperator.Unspecified => null,
            BitDataGridFilterOperator.IsEmpty => BuildEmptiness(member, negate: false),
            BitDataGridFilterOperator.IsNotEmpty => BuildEmptiness(member, negate: true),
            BitDataGridFilterOperator.Contains or BitDataGridFilterOperator.DoesNotContain
                or BitDataGridFilterOperator.StartsWith or BitDataGridFilterOperator.EndsWith
                => BuildStringMatch(member, filter),
            _ => BuildComparison(accessor, member, filter),
        };

        return body is null ? null : Expression.Lambda<Func<TItem, bool>>(body, param);
    }

    private static Expression? BuildEmptiness(Expression member, bool negate)
    {
        Expression empty;
        if (member.Type == typeof(string))
        {
            empty = Expression.OrElse(
                Expression.Equal(member, Expression.Constant(null, typeof(string))),
                Expression.Equal(member, Expression.Constant(string.Empty)));
        }
        else if (!member.Type.IsValueType || Nullable.GetUnderlyingType(member.Type) is not null)
        {
            empty = Expression.Equal(member, Expression.Constant(null, member.Type));
        }
        else
        {
            // A non-nullable value type is never "empty".
            empty = Expression.Constant(false);
        }
        return negate ? Expression.Not(empty) : empty;
    }

    private static Expression? BuildStringMatch(Expression member, BitDataGridFilterDescriptor filter)
    {
        if (member.Type != typeof(string)) return null;
        var term = filter.Value?.ToString();
        if (string.IsNullOrWhiteSpace(term)) return null;

        // Case-insensitive via ToLower(), which LINQ providers translate (e.g. SQL LOWER()). The term
        // is lowered with the same culture-aware ToLower() so an in-memory queryable applies identical
        // casing rules to both sides (invariant lowering would mismatch e.g. the Turkish dotless I).
        var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var methodName = filter.Operator switch
        {
            BitDataGridFilterOperator.StartsWith => nameof(string.StartsWith),
            BitDataGridFilterOperator.EndsWith => nameof(string.EndsWith),
            _ => nameof(string.Contains),
        };
        var match = (Expression)Expression.Call(
            Expression.Call(member, toLower),
            typeof(string).GetMethod(methodName, new[] { typeof(string) })!,
            Expression.Constant(term.ToLower()));

        var isNull = Expression.Equal(member, Expression.Constant(null, typeof(string)));
        return filter.Operator == BitDataGridFilterOperator.DoesNotContain
            // A null value trivially "does not contain" the term (matching the in-memory pipeline).
            ? Expression.OrElse(isNull, Expression.Not(match))
            : Expression.AndAlso(Expression.Not(isNull), match);
    }

    private static Expression? BuildComparison<TItem>(
        BitDataGridPropertyAccessor<TItem> accessor, Expression member, BitDataGridFilterDescriptor filter)
    {
        if (filter.Value is string s && string.IsNullOrWhiteSpace(s)) return null;

        if (filter.Value is null)
        {
            // Null operand: only (in)equality is meaningful, and only for nullable member types.
            if (member.Type.IsValueType && Nullable.GetUnderlyingType(member.Type) is null) return null;
            return filter.Operator switch
            {
                BitDataGridFilterOperator.Equals => Expression.Equal(member, Expression.Constant(null, member.Type)),
                BitDataGridFilterOperator.NotEquals => Expression.NotEqual(member, Expression.Constant(null, member.Type)),
                _ => null,
            };
        }

        // Coerce the operand to the member's type the same way edits/in-memory filters do; an
        // uncoercible operand (malformed input) produces no predicate rather than a provider error.
        if (!accessor.TryConvertValue(filter.Value, out var converted) || converted is null) return null;
        var operand = Expression.Constant(converted, member.Type);

        // The date filter editor emits a calendar day at midnight; the in-memory pipeline gives
        // Equals/NotEquals on DateTime/DateTimeOffset whole-day semantics for such operands
        // (BitDataGridDataProcessor.TryDateOnlyEquals). Mirror that here so paging/export over an
        // IQueryable source matches the descriptor contract instead of comparing exact timestamps.
        if (filter.Operator is BitDataGridFilterOperator.Equals or BitDataGridFilterOperator.NotEquals
            && BuildDayEquality(member, converted, negate: filter.Operator == BitDataGridFilterOperator.NotEquals) is { } dayBody)
        {
            return dayBody;
        }

        try
        {
            return filter.Operator switch
            {
                BitDataGridFilterOperator.Equals => Expression.Equal(member, operand),
                BitDataGridFilterOperator.NotEquals => Expression.NotEqual(member, operand),
                BitDataGridFilterOperator.GreaterThan => Expression.GreaterThan(member, operand),
                BitDataGridFilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, operand),
                BitDataGridFilterOperator.LessThan => Expression.LessThan(member, operand),
                BitDataGridFilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(member, operand),
                _ => null,
            };
        }
        catch (InvalidOperationException)
        {
            // The member type defines no such comparison operator (e.g. ordering a Guid); skip the
            // filter instead of surfacing an expression-construction error.
            return null;
        }
    }

    // Builds a calendar-day (in)equality for a DateTime/DateTimeOffset member and a midnight operand,
    // via the .Date property (which LINQ providers translate, e.g. EF Core → CONVERT(date, ...));
    // DateTimeOffset.Date uses the row's own offset, matching the in-memory behavior. Returns null
    // when the operand isn't a date-only value for such a member, so the caller falls back to the
    // exact comparison. A null row value is never equal to a day (and always not-equal), matching the
    // in-memory comparer.
    private static Expression? BuildDayEquality(Expression member, object converted, bool negate)
    {
        var underlying = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

        DateTime day;
        if (underlying == typeof(DateTime) && converted is DateTime dt && dt.TimeOfDay == TimeSpan.Zero)
            day = dt.Date;
        else if (underlying == typeof(DateTimeOffset) && converted is DateTimeOffset dto && dto.TimeOfDay == TimeSpan.Zero)
            day = dto.Date;
        else
            return null;

        var nullable = member.Type != underlying;
        var valueExpr = nullable ? Expression.Property(member, "Value") : member;
        var dateExpr = Expression.Property(valueExpr, nameof(DateTime.Date));
        var dayConst = Expression.Constant(day, typeof(DateTime));

        if (!nullable)
            return negate ? Expression.NotEqual(dateExpr, dayConst) : Expression.Equal(dateExpr, dayConst);

        var hasValue = Expression.Property(member, "HasValue");
        return negate
            ? Expression.OrElse(Expression.Not(hasValue), Expression.NotEqual(dateExpr, dayConst))
            : Expression.AndAlso(hasValue, Expression.Equal(dateExpr, dayConst));
    }
}
