using System.Linq.Expressions;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Decides which index, if any, a query should read from.
///
/// The rules are deliberately shallow - there are no statistics to consult and every row is
/// already in memory, so the win is asymptotic rather than marginal: an equality lookup or a range
/// scan instead of touching every row. Preference runs primary-key equality (or membership) first,
/// then compound equality (most selective index), then single-property equality or membership, then a range, and
/// finally an ordering that an ordered index can serve for free. Anything not turned into a source
/// stays a residual filter - and so does a test the source answers only approximately: one against
/// an index with a comparer of its own, or over floating-point values, where NaN is equal to itself
/// and smallest of all to an index but to no C# operator (see <see cref="BlazeDbIndex.Exact"/>).
/// </summary>
internal static class BlazeDbIndexSelector
{
    public static BlazeDbIndexSelection Choose(
        BlazeDbQueryPlan plan,
        IBlazeDbTableBinding binding,
        List<LambdaExpression> predicates,
        List<MethodCallExpression> orderings,
        bool useSecondaryIndexes = true)
    {
        if (predicates.Count == 0 && orderings.Count == 0)
        {
            return new BlazeDbIndexSelection([], OrderingSatisfied: false);
        }

        var conjuncts = BlazeDbConjuncts.Flatten(predicates, out var parameter);
        var comparisons = conjuncts
            .Select((c, i) => (Index: i, Comparison: BlazeDbComparison.TryRead(c, parameter)))
            .Where(x => x.Comparison is not null)
            .ToList();

        var used = new HashSet<int>();
        string? rangeMember = null;

        if (!TryPrimaryKey(plan, binding, comparisons, used) && useSecondaryIndexes &&
            !TryCompoundEquality(plan, binding, comparisons, used) &&
            !TrySingleEquality(plan, binding, comparisons, used))
        {
            rangeMember = TryRange(plan, binding, comparisons, used);
        }

        var orderingSatisfied = useSecondaryIndexes && TrySatisfyOrdering(plan, binding, orderings, rangeMember);

        return new BlazeDbIndexSelection(BlazeDbConjuncts.Rebuild(conjuncts, used, parameter, predicates), orderingSatisfied);
    }

    private static bool TryPrimaryKey(
        BlazeDbQueryPlan plan, IBlazeDbTableBinding binding, List<(int Index, BlazeDbComparison? Comparison)> comparisons,
        HashSet<int> used)
    {
        if (binding.KeyMember is null)
        {
            return false;
        }
        // Equality first: one probe beats a list of them, and a list is not tried when both appear.
        foreach (var membership in (bool[])[false, true])
        {
            foreach (var (i, comparison) in comparisons)
            {
                if (comparison!.Member != binding.KeyMember || comparison.IsMembership != membership)
                {
                    continue;
                }
                if (membership)
                {
                    // No row has a null key, so a null in the list matches nothing and is dropped.
                    plan.UseKeys(comparison.InValues!.Where(static key => key is not null).ToArray()!);
                }
                else if (comparison.Operator == ExpressionType.Equal && comparison.Value is not null)
                {
                    plan.UseKey(comparison.Value);
                }
                else
                {
                    continue;
                }
                // The key dictionary compares by default equality, which only a floating-point key
                // can tell apart from ==: a NaN key finds the row whose key is NaN.
                if (!BlazeDbIndex.HasFloatingPoint(binding.KeyType))
                {
                    used.Add(i);
                }
                return true;
            }
        }
        return false;
    }

    private static bool TryCompoundEquality(
        BlazeDbQueryPlan plan, IBlazeDbTableBinding binding, List<(int Index, BlazeDbComparison? Comparison)> comparisons,
        HashSet<int> used)
    {
        foreach (var index in binding.Indexes.Where(i => i.Members.Count > 1).OrderByDescending(i => i.Members.Count))
        {
            var matched = new List<(int Index, BlazeDbComparison Comparison)>();
            // The tuple's element types say which members may be compared against null: a reference
            // or Nullable<T> element can hold one, and the tuple then compares it as a value.
            var elementTypes = index.KeyType.GetGenericArguments();
            for (var position = 0; position < index.Members.Count; position++)
            {
                var member = index.Members[position];
                var nullable = position < elementTypes.Length && CanHoldNull(elementTypes[position]);
                var match = comparisons.FirstOrDefault(c =>
                    c.Comparison!.Operator == ExpressionType.Equal &&
                    c.Comparison.Member == member &&
                    !c.Comparison.IsMembership &&
                    (c.Comparison.Value is not null || nullable) &&
                    !matched.Any(m => m.Index == c.Index));
                if (match.Comparison is null)
                {
                    matched.Clear();
                    break;
                }
                matched.Add((match.Index, match.Comparison));
            }
            if (matched.Count != index.Members.Count)
            {
                continue;
            }

            // The engine indexes the tuple of the members, so the lookup key is that same tuple; each
            // value already has its member's type, which is the type the tuple declares for it.
            var key = Activator.CreateInstance(index.KeyType, matched.Select(m => m.Comparison.Value).ToArray());
            if (key is null)
            {
                continue;
            }

            plan.UseEquality(index, key);
            if (index.Exact)
            {
                foreach (var (i, _) in matched)
                {
                    used.Add(i);
                }
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// A single-property equality, or a membership list, against a hash index - or an ordered one,
    /// which answers equality as a range of one key, still far better than a scan. A comparison
    /// against null is answered by the index too, since it records the rows whose value is null -
    /// provided the index's key type can hold a null at all. On a non-nullable property the
    /// comparison is left as a filter, which correctly matches nothing.
    /// </summary>
    private static bool TrySingleEquality(
        BlazeDbQueryPlan plan, IBlazeDbTableBinding binding, List<(int Index, BlazeDbComparison? Comparison)> comparisons,
        HashSet<int> used)
    {
        foreach (var membership in (bool[])[false, true])
        {
            foreach (var (i, comparison) in comparisons)
            {
                if (comparison!.IsMembership != membership ||
                    (!membership && comparison.Operator != ExpressionType.Equal))
                {
                    continue;
                }
                // A membership list is a lookup per value, and two values a comparer of the index's
                // own counts as one ("A" and "a", ignoring case) would bring the same rows back twice.
                var index = Single(binding, comparison.Member, ordered: false, defaultComparerOnly: membership)
                            ?? Single(binding, comparison.Member, ordered: true, defaultComparerOnly: membership);
                if (index is null)
                {
                    continue;
                }
                var nullable = CanHoldNull(index.KeyType);
                if (membership)
                {
                    // A null the index cannot hold matches no row, so it is simply left out.
                    plan.UseEqualityIn(index, nullable ? comparison.InValues! : comparison.InValues!.Where(static key => key is not null).ToArray());
                }
                else if (comparison.Value is not null || nullable)
                {
                    plan.UseEquality(index, comparison.Value);
                }
                else
                {
                    continue;
                }
                if (index.Exact)
                {
                    used.Add(i);
                }
                return true;
            }
        }
        return false;
    }

    private static bool CanHoldNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    /// <summary>Returns the member the chosen range covers, or null when no range was chosen.</summary>
    private static string? TryRange(
        BlazeDbQueryPlan plan, IBlazeDbTableBinding binding, List<(int Index, BlazeDbComparison? Comparison)> comparisons,
        HashSet<int> used)
    {
        foreach (var group in comparisons.Where(c => c.Comparison!.IsRange).GroupBy(c => c.Comparison!.Member))
        {
            // An index ordered by a comparer of its own holds the range's rows somewhere other than
            // between its bounds, so it cannot even narrow the search.
            var index = Single(binding, group.Key, ordered: true);
            if (index is not { DefaultComparer: true })
            {
                continue;
            }

            object? from = null, to = null;
            bool hasFrom = false, hasTo = false;
            var usedHere = new List<int>();
            foreach (var (i, comparison) in group)
            {
                if (comparison!.Value is not { } bound)
                {
                    continue;
                }
                var lower = comparison.Operator is ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual;
                if (lower && !hasFrom)
                {
                    (hasFrom, from) = (true, bound);
                }
                else if (!lower && !hasTo)
                {
                    (hasTo, to) = (true, bound);
                }
                else
                {
                    continue;
                }

                // The engine's ranges are inclusive at both ends, so a strict comparison keeps its
                // conjunct as a residual filter that trims the boundary rows. So does any comparison
                // over floating-point values, whose NaNs sort below the lower end of an open range.
                if (index.Exact && comparison.Operator is ExpressionType.GreaterThanOrEqual or ExpressionType.LessThanOrEqual)
                {
                    usedHere.Add(i);
                }
            }

            if (hasFrom || hasTo)
            {
                plan.UseRange(index, hasFrom, from, hasTo, to, descending: false);
                used.UnionWith(usedHere);
                return group.Key;
            }
        }
        return null;
    }

    private static bool TrySatisfyOrdering(
        BlazeDbQueryPlan plan, IBlazeDbTableBinding binding, List<MethodCallExpression> orderings, string? rangeMember)
    {
        // Only a single OrderBy can be answered by an index: a ThenBy would need the index to be
        // compound over exactly the same members, which the ordering syntax cannot express here.
        if (orderings.Count != 1)
        {
            return false;
        }

        var call = orderings[0];
        var selector = BlazeDbComparison.StripQuotes(call.Arguments[1]) as LambdaExpression;
        if (selector?.Body is not MemberExpression member || member.Expression != selector.Parameters[0])
        {
            return false;
        }

        var descending = call.Method.Name is nameof(Queryable.OrderByDescending);

        if (plan.HasIndexSource)
        {
            // A range over the ordering's own member already walks the index in order; all that is
            // left is to walk it in the requested direction instead of sorting afterwards.
            if (rangeMember == member.Member.Name)
            {
                plan.SetRangeDirection(descending);
                return true;
            }
            return false;
        }

        // Walking an index answers an ordering only in the order the type itself sorts in.
        var index = Single(binding, member.Member.Name, ordered: true);
        if (index is not { DefaultComparer: true })
        {
            return false;
        }

        plan.UseRange(index, hasFrom: false, null, hasTo: false, null, descending);
        return true;
    }

    private static BlazeDbIndex? Single(IBlazeDbTableBinding binding, string member, bool ordered, bool defaultComparerOnly = false) =>
        binding.Indexes.FirstOrDefault(i =>
            i.Ordered == ordered && i.Members.Count == 1 && i.Members[0] == member &&
            (i.DefaultComparer || !defaultComparerOnly));
}
