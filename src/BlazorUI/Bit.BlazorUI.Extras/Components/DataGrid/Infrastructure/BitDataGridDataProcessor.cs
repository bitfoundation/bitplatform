using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Client-side data pipeline: filtering, multi-sorting, grouping and aggregation.
/// </summary>
public static class BitDataGridDataProcessor
{
    public static IReadOnlyList<TItem> Filter<TItem>(
        IEnumerable<TItem> source,
        IReadOnlyList<BitDataGridFilterDescriptor> filters,
        IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns)
    {
        if (filters.Count == 0)
            return source as IReadOnlyList<TItem> ?? source.ToList();

        var query = source;
        foreach (var filter in filters)
        {
            if (!columns.TryGetValue(filter.ColumnId, out var column) || column.Accessor is null)
                continue;
            var f = filter;
            var col = column;
            query = query.Where(item => Matches(col.Accessor!.GetValue(item), f));
        }
        return query.ToList();
    }

    public static IReadOnlyList<TItem> Sort<TItem>(
        IReadOnlyList<TItem> source,
        IReadOnlyList<BitDataGridSortDescriptor> sorts,
        IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns)
    {
        var active = sorts.Where(s => s.Direction != BitDataGridSortDirection.None).OrderBy(s => s.Priority).ToList();
        if (active.Count == 0) return source;

        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var sort in active)
        {
            // SortKey resolves the column's custom SortBy selector when present, falling back to the
            // field accessor - so template-only columns with a SortBy participate in sorting too.
            if (!columns.TryGetValue(sort.ColumnId, out var column) || column.SortKey is not { } key)
                continue;
            var comparer = BitDataGridValueComparer.Instance;
            if (ordered is null)
            {
                ordered = sort.Direction == BitDataGridSortDirection.Ascending
                    ? source.OrderBy(key, comparer)
                    : source.OrderByDescending(key, comparer);
            }
            else
            {
                ordered = sort.Direction == BitDataGridSortDirection.Ascending
                    ? ordered.ThenBy(key, comparer)
                    : ordered.ThenByDescending(key, comparer);
            }
        }
        return ordered?.ToList() ?? source;
    }

    public static List<BitDataGridGroup<TItem>> Group<TItem>(
        IReadOnlyList<TItem> source,
        IReadOnlyList<BitDataGridGroupDescriptor> groups,
        IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns)
    {
        if (groups.Count == 0) return new List<BitDataGridGroup<TItem>>();

        // Drop stale descriptors that no longer map to a field-backed column up front, mirroring the
        // filter/sort paths that silently ignore unknown columns. Without this, a single invalid
        // descriptor would short-circuit BuildGroups and blank the entire grouped view instead of
        // degrading gracefully to the remaining valid groupings.
        var valid = groups
            .Where(g => columns.TryGetValue(g.ColumnId, out var c) && c.Accessor is not null)
            .ToList();
        if (valid.Count == 0) return new List<BitDataGridGroup<TItem>>();

        return BuildGroups(source, valid, columns, 0, string.Empty);
    }

    private static List<BitDataGridGroup<TItem>> BuildGroups<TItem>(
        IReadOnlyList<TItem> source,
        IReadOnlyList<BitDataGridGroupDescriptor> groups,
        IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns,
        int level,
        string parentPath)
    {
        var result = new List<BitDataGridGroup<TItem>>();
        var descriptor = groups[level];
        if (!columns.TryGetValue(descriptor.ColumnId, out var column) || column.Accessor is null)
            return result;

        var grouped = source
            .GroupBy(item => column.Accessor!.GetValue(item), BitDataGridValueEqualityComparer.Instance)
            .Select(g =>
            {
                var keyText = column.FormatValue(g.Key);
                var items = g.ToList();
                // Use a culture-invariant, type-qualified identifier for the path so that distinct keys
                // never collide in collapse/expand state regardless of the current culture's formatting
                // (e.g. "1,5" vs "1.5") or display text shared across different key types.
                var keyId = g.Key switch
                {
                    null => "∅",
                    IFormattable f => $"{g.Key.GetType().FullName ?? g.Key.GetType().Name}:{f.ToString(null, CultureInfo.InvariantCulture)}",
                    _ => $"{g.Key.GetType().FullName ?? g.Key.GetType().Name}:{g.Key}"
                };
                // Include the grouping column id in the path so the collapse/expand state is scoped to
                // the column that produced the group. Without it, changing the grouped column would let
                // a same-valued key at the same level reuse another column's stale expansion state.
                var path = $"{parentPath}/{level}:{descriptor.ColumnId}:{keyId}";
                var isLeaf = level + 1 >= groups.Count;
                // Build the nested collections up front and assign them through the (read-only) init
                // members, so the public group shape stays immutable instead of being mutated via AddRange
                // after construction.
                var group = new BitDataGridGroup<TItem>
                {
                    ColumnId = descriptor.ColumnId,
                    Key = g.Key,
                    KeyText = keyText,
                    Level = level,
                    Path = path,
                    Count = items.Count,
                    // Only leaf groups retain the row list; parent groups rely on Count/Aggregates/SubGroups
                    // so a row isn't referenced again on every ancestor level.
                    Items = isLeaf ? items : new List<TItem>(),
                    SubGroups = isLeaf ? new List<BitDataGridGroup<TItem>>() : BuildGroups(items, groups, columns, level + 1, path),
                    Aggregates = Aggregate(items, columns.Values)
                };
                return group;
            });

        grouped = descriptor.Direction switch
        {
            // None: preserve the original group encounter order rather than implicitly sorting ascending.
            BitDataGridSortDirection.None => grouped,
            BitDataGridSortDirection.Descending => grouped.OrderByDescending(g => g.Key, BitDataGridValueComparer.Instance),
            _ => grouped.OrderBy(g => g.Key, BitDataGridValueComparer.Instance)
        };

        result = grouped.ToList();
        return result;
    }

    public static List<BitDataGridAggregateResult> Aggregate<TItem>(
        IReadOnlyList<TItem> source,
        IEnumerable<BitDataGridColumn<TItem>> columns)
    {
        var results = new List<BitDataGridAggregateResult>();
        foreach (var column in columns)
        {
            // A custom AggregateBy participates regardless of the declarative Aggregate type (and needs
            // no field accessor); built-in aggregations require both a type and a bound field.
            if (column.AggregateBy is null && (column.Aggregate == BitDataGridAggregateType.None || column.Accessor is null)) continue;
            var value = column.AggregateBy is not null ? column.AggregateBy(source) : ComputeAggregate(source, column);
            var format = column.AggregateFormat ?? column.Format;
            var formatted = value is IFormattable fmt && !string.IsNullOrEmpty(format)
                ? fmt.ToString(format, CultureInfo.CurrentCulture)
                : value?.ToString() ?? string.Empty;
            results.Add(new BitDataGridAggregateResult
            {
                ColumnId = column.Id,
                // An AggregateBy value isn't the product of the declarative Aggregate function (which
                // may even be None), so report it as Custom instead of echoing a type it didn't run.
                Type = column.AggregateBy is not null ? BitDataGridAggregateType.Custom : column.Aggregate,
                Value = value,
                FormattedValue = formatted
            });
        }
        return results;
    }

    private static object? ComputeAggregate<TItem>(IReadOnlyList<TItem> source, BitDataGridColumn<TItem> column)
    {
        var accessor = column.Accessor!;
        switch (column.Aggregate)
        {
            case BitDataGridAggregateType.Count:
                return source.Count;
            case BitDataGridAggregateType.Sum:
            case BitDataGridAggregateType.Average:
            {
                decimal sum = 0; int n = 0;
                foreach (var item in source)
                {
                    if (TryToDecimal(accessor.GetValue(item), out var d)) { sum += d; n++; }
                }
                if (column.Aggregate == BitDataGridAggregateType.Sum) return sum;
                return n == 0 ? 0m : sum / n;
            }
            case BitDataGridAggregateType.Min:
            case BitDataGridAggregateType.Max:
            {
                object? best = null;
                foreach (var item in source)
                {
                    var v = accessor.GetValue(item);
                    if (v is null) continue;
                    if (best is null) { best = v; continue; }
                    var cmp = BitDataGridValueComparer.Instance.Compare(v, best);
                    if (column.Aggregate == BitDataGridAggregateType.Min ? cmp < 0 : cmp > 0) best = v;
                }
                return best;
            }
            default:
                return null;
        }
    }

    private static bool TryToDecimal(object? value, out decimal result)
    {
        result = 0;
        if (value is null) return false;
        try { result = Convert.ToDecimal(value, CultureInfo.InvariantCulture); return true; }
        catch { return false; }
    }

    private static bool Matches(object? value, BitDataGridFilterDescriptor filter)
    {
        switch (filter.Operator)
        {
            case BitDataGridFilterOperator.Unspecified:
                // No operator selected: treat the filter as omitted so it doesn't exclude any rows.
                return true;
            case BitDataGridFilterOperator.IsEmpty:
                return value is null || string.IsNullOrEmpty(value.ToString());
            case BitDataGridFilterOperator.IsNotEmpty:
                return value is not null && !string.IsNullOrEmpty(value.ToString());
        }

        // An empty or whitespace-only string filter value carries no criteria, so treat it like an
        // omitted filter and match every row. A *null* filter value, however, is a meaningful operand:
        // it must flow into the numeric/comparable branch below so Equals/NotEquals can distinguish null
        // rows from non-null rows (and string operators handle null via their own null guard).
        if (filter.Value is string blank && string.IsNullOrWhiteSpace(blank))
            return true;

        // Numeric / comparable operators
        if (filter.Operator is BitDataGridFilterOperator.GreaterThan or BitDataGridFilterOperator.GreaterThanOrEqual
            or BitDataGridFilterOperator.LessThan or BitDataGridFilterOperator.LessThanOrEqual
            or BitDataGridFilterOperator.Equals or BitDataGridFilterOperator.NotEquals)
        {
            // Handle nulls explicitly rather than letting the comparer order nulls-first, which would
            // otherwise make a null row value spuriously match LessThan/LessThanOrEqual filters.
            if (value is null)
            {
                return filter.Operator switch
                {
                    BitDataGridFilterOperator.Equals => filter.Value is null,
                    BitDataGridFilterOperator.NotEquals => filter.Value is not null,
                    _ => false
                };
            }

            if (filter.Value is null)
            {
                // Row value is non-null here, so equality against a null filter value is deterministic;
                // ordering operators have no meaningful null operand, so they don't match.
                return filter.Operator switch
                {
                    BitDataGridFilterOperator.Equals => false,
                    BitDataGridFilterOperator.NotEquals => true,
                    _ => false
                };
            }

            // Coerce the filter operand to the row value's runtime type once, up front. If it can't be
            // coerced (e.g. a malformed number/date/Guid typed into the filter box), there is no
            // meaningful comparison: fail closed rather than letting the comparer order mixed types by a
            // type-name discriminator, which would otherwise make every row spuriously match an ordering
            // operator (e.g. "Price < abc"). Only NotEquals stays true, since the values aren't provably
            // equal.
            if (!TryCoerceToValueType(value, filter.Value, out var operand))
            {
                return filter.Operator is BitDataGridFilterOperator.NotEquals;
            }

            // The date filter editor emits a calendar day at midnight (no time component). Comparing it
            // with strict equality against a DateTime/DateTimeOffset row value that carries a time-of-day
            // would never match, so equality filters on those types are evaluated on the calendar day
            // only (day-range semantics). DateOnly has no time component and keeps its existing behavior.
            if (filter.Operator is BitDataGridFilterOperator.Equals or BitDataGridFilterOperator.NotEquals
                && TryDateOnlyEquals(value, operand, out var sameDay))
            {
                return filter.Operator is BitDataGridFilterOperator.Equals ? sameDay : !sameDay;
            }

            var cmp = BitDataGridValueComparer.Instance.Compare(value, operand);
            return filter.Operator switch
            {
                BitDataGridFilterOperator.GreaterThan => cmp > 0,
                BitDataGridFilterOperator.GreaterThanOrEqual => cmp >= 0,
                BitDataGridFilterOperator.LessThan => cmp < 0,
                BitDataGridFilterOperator.LessThanOrEqual => cmp <= 0,
                BitDataGridFilterOperator.Equals => cmp == 0,
                BitDataGridFilterOperator.NotEquals => cmp != 0,
                _ => true
            };
        }

        if (filter.Value is null)
            return true;

        // String operators
        var text = value?.ToString() ?? string.Empty;
        var term = filter.Value.ToString() ?? string.Empty;
        return filter.Operator switch
        {
            BitDataGridFilterOperator.Contains => text.Contains(term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.DoesNotContain => !text.Contains(term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.StartsWith => text.StartsWith(term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.EndsWith => text.EndsWith(term, StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    // Returns true (via <paramref name="equal"/>) when a DateTime/DateTimeOffset row value falls on the
    // same calendar day as a date-only filter value (one whose time component is midnight). Used so an
    // equality filter coming from the date editor matches the whole day instead of an exact timestamp.
    // Returns false when the operands aren't a date/time pair carrying a midnight filter value, leaving
    // the caller to fall back to the normal exact comparison.
    private static bool TryDateOnlyEquals(object? value, object? filterValue, out bool equal)
    {
        equal = false;
        if (value is DateTime vdt && filterValue is DateTime fdt && fdt.TimeOfDay == TimeSpan.Zero)
        {
            equal = vdt.Date == fdt.Date;
            return true;
        }
        if (value is DateTimeOffset vdto && filterValue is DateTimeOffset fdto && fdto.TimeOfDay == TimeSpan.Zero)
        {
            equal = vdto.Date == fdto.Date;
            return true;
        }
        return false;
    }

    // Coerces a filter operand to the row value's runtime type before comparison. This mirrors the
    // type-specific parsing in BitDataGridPropertyAccessor.TryConvertValue (Guid/DateOnly/TimeOnly/
    // DateTimeOffset and enums are not handled by Convert.ChangeType), so a filter value entered as a
    // string is converted to the property's real type the same way edits are - keeping filtering,
    // sorting and editing consistent. Parsing uses the invariant culture to match the ISO/invariant
    // strings the editors emit. Returns false when the operand can't be turned into the target type, so
    // callers fail the comparison closed instead of comparing mixed types (which the value comparer
    // would otherwise order by a meaningless type-name discriminator).
    private static bool TryCoerceToValueType(object? sample, object filterValue, out object? coerced)
    {
        coerced = filterValue;
        if (sample is null) return true;
        var target = Nullable.GetUnderlyingType(sample.GetType()) ?? sample.GetType();
        if (target.IsInstanceOfType(filterValue)) return true;
        try
        {
            if (target.IsEnum)
                coerced = filterValue is string es ? Enum.Parse(target, es, true) : Enum.ToObject(target, filterValue);
            else if (target == typeof(Guid))
                coerced = filterValue is Guid g ? g : Guid.Parse(filterValue.ToString()!);
            else if (target == typeof(DateOnly))
                coerced = filterValue is DateOnly d ? d : DateOnly.Parse(filterValue.ToString()!, CultureInfo.InvariantCulture);
            else if (target == typeof(TimeOnly))
                coerced = filterValue is TimeOnly t ? t : TimeOnly.Parse(filterValue.ToString()!, CultureInfo.InvariantCulture);
            else if (target == typeof(DateTimeOffset))
                // DateTimeOffset is not IConvertible, so Convert.ChangeType would throw for it; parse it
                // explicitly like the property accessor does.
                coerced = filterValue is DateTimeOffset dto ? dto : DateTimeOffset.Parse(filterValue.ToString()!, CultureInfo.InvariantCulture);
            else
                coerced = Convert.ChangeType(filterValue, target, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            coerced = null;
            return false;
        }
    }
}
