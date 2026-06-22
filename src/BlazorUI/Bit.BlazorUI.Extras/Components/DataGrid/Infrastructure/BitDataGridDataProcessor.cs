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
            if (!columns.TryGetValue(sort.ColumnId, out var column) || column.Accessor is null)
                continue;
            var accessor = column.Accessor;
            Func<TItem, object?> key = item => accessor.GetValue(item);
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
        return BuildGroups(source, groups, columns, 0, string.Empty);
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
            .GroupBy(item => column.Accessor!.GetValue(item))
            .Select(g =>
            {
                var keyText = column.FormatValue(g.Key);
                var items = g.ToList();
                // Use the raw key (not the formatted display text) for the path identifier so that
                // distinct keys producing identical display values don't collide and share collapse/expand state.
                var path = $"{parentPath}/{level}:{g.Key}";
                var group = new BitDataGridGroup<TItem>
                {
                    ColumnId = descriptor.ColumnId,
                    Key = g.Key,
                    KeyText = keyText,
                    Level = level,
                    Path = path,
                    Items = items
                };
                if (level + 1 < groups.Count)
                    group.SubGroups.AddRange(BuildGroups(items, groups, columns, level + 1, path));
                group.Aggregates.AddRange(Aggregate(items, columns.Values));
                return group;
            });

        grouped = descriptor.Direction == BitDataGridSortDirection.Descending
            ? grouped.OrderByDescending(g => g.Key, BitDataGridValueComparer.Instance)
            : grouped.OrderBy(g => g.Key, BitDataGridValueComparer.Instance);

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
            if (column.Aggregate == BitDataGridAggregateType.None || column.Accessor is null) continue;
            var value = ComputeAggregate(source, column);
            var format = column.AggregateFormat ?? column.Format;
            var formatted = value is IFormattable fmt && !string.IsNullOrEmpty(format)
                ? fmt.ToString(format, CultureInfo.CurrentCulture)
                : value?.ToString() ?? string.Empty;
            results.Add(new BitDataGridAggregateResult
            {
                ColumnId = column.Id,
                Type = column.Aggregate,
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
            case BitDataGridFilterOperator.IsEmpty:
                return value is null || string.IsNullOrEmpty(value.ToString());
            case BitDataGridFilterOperator.IsNotEmpty:
                return value is not null && !string.IsNullOrEmpty(value.ToString());
        }

        if (filter.Value is null)
            return true;

        // Numeric / comparable operators
        if (filter.Operator is BitDataGridFilterOperator.GreaterThan or BitDataGridFilterOperator.GreaterThanOrEqual
            or BitDataGridFilterOperator.LessThan or BitDataGridFilterOperator.LessThanOrEqual
            or BitDataGridFilterOperator.Equals or BitDataGridFilterOperator.NotEquals)
        {
            var cmp = BitDataGridValueComparer.Instance.Compare(value, CoerceToValueType(value, filter.Value));
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

    private static object? CoerceToValueType(object? sample, object filterValue)
    {
        if (sample is null) return filterValue;
        var target = Nullable.GetUnderlyingType(sample.GetType()) ?? sample.GetType();
        if (target.IsInstanceOfType(filterValue)) return filterValue;
        try
        {
            if (target.IsEnum)
                return filterValue is string s ? Enum.Parse(target, s, true) : Enum.ToObject(target, filterValue);
            return Convert.ChangeType(filterValue, target, CultureInfo.CurrentCulture);
        }
        catch { return filterValue; }
    }
}
