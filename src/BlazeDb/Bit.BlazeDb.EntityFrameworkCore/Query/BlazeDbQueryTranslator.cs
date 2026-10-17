using System.Linq.Expressions;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Splits a LINQ query into the part the engine can answer and the part it cannot.
///
/// The engine understands a source (a scan or an index lookup), residual filters, an ordering and
/// paging - so the translator consumes the longest prefix of the operator chain that fits that
/// shape, choosing an index where a predicate or an ordering allows one. Whatever is left, from a
/// projection to a group-by, is handed back untouched and runs as LINQ to Objects over the rows
/// the plan produced. Rows are live objects in memory, so that fallback costs nothing but the
/// enumeration itself, and no query is ever rejected for being untranslatable.
/// </summary>
internal static class BlazeDbQueryTranslator
{
    // useIndexes: false makes every predicate a filter and every ordering a sort, for the moments the
    // secondary indexes cannot be trusted - see BlazeDbQueryExecutor. A primary-key lookup is still taken.
    public static BlazeDbTranslatedQuery Translate(
        Expression expression, IBlazeDbTableBinding binding, Type entityClrType, bool useIndexes = true)
    {
        var chain = Decompose(expression);
        var plan = binding.CreatePlan();

        var predicates = new List<LambdaExpression>();
        var orderings = new List<MethodCallExpression>();
        var consumed = 0;
        var stage = Stage.Shape;

        foreach (var call in chain)
        {
            var name = call.Method.Name;
            var next = StageOf(name);
            if (next is null || next < stage)
            {
                break;
            }

            switch (name)
            {
                case nameof(Queryable.Where):
                    if (call.Arguments.Count != 2 || Lambda(call, 1).Parameters.Count != 1)
                    {
                        goto done;
                    }
                    predicates.Add(Lambda(call, 1));
                    break;

                case nameof(Queryable.OrderBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.ThenByDescending):
                    if (call.Arguments.Count != 2)
                    {
                        goto done;
                    }
                    orderings.Add(call);
                    break;

                case nameof(Queryable.Skip):
                    // A Skip after a Take changes which rows the Take applies to; the plan applies
                    // Skip first, so it cannot absorb one. Consecutive Skips add up.
                    if (plan.Take >= 0 || !TryConstantInt(call.Arguments[1], out var skip))
                    {
                        goto done;
                    }
                    plan.Skip = (int)Math.Min((long)plan.Skip + Math.Max(0, skip), int.MaxValue);
                    break;

                case nameof(Queryable.Take):
                    if (!TryConstantInt(call.Arguments[1], out var take))
                    {
                        goto done;
                    }
                    // LINQ treats a negative count as zero; the plan uses -1 for "no limit", and
                    // a second Take can only shrink the first.
                    take = Math.Max(0, take);
                    plan.Take = plan.Take < 0 ? take : Math.Min(plan.Take, take);
                    break;

                default:
                    goto done;
            }

            stage = next.Value;
            consumed++;
        }

    done:
        // Index selection happens once the whole prefix is known, so an ordering can influence it:
        // an OrderBy over an indexed property is answered by walking that index instead of sorting.
        var selection = BlazeDbIndexSelector.Choose(plan, binding, predicates, orderings, useIndexes);
        foreach (var predicate in selection.Residual)
        {
            plan.AddPredicate(predicate);
        }
        if (!selection.OrderingSatisfied)
        {
            foreach (var ordering in orderings)
            {
                plan.AddOrdering(ordering);
            }
        }

        return new BlazeDbTranslatedQuery(plan, RebuildRemainder(chain, consumed, entityClrType));
    }

    /// <summary>
    /// Operator bands, in the order the engine applies them. A chain is only consumed while it does
    /// not go back a band: filtering and sorting commute with each other (a plan filters, then sorts,
    /// whichever way they were written) so they share one band, but paging pins the order of what
    /// follows - <c>Take(5).Where(...)</c> does not mean <c>Where(...).Take(5)</c> - so a filter or
    /// sort after paging is left for the fallback.
    /// </summary>
    private enum Stage
    {
        Shape = 0,
        Page = 1,
    }

    private static Stage? StageOf(string methodName) => methodName switch
    {
        nameof(Queryable.Where) => Stage.Shape,
        nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending) => Stage.Shape,
        // ThenBy composes onto the ordering the plan already has.
        nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending) => Stage.Shape,
        nameof(Queryable.Skip) or nameof(Queryable.Take) => Stage.Page,
        _ => null,
    };

    /// <summary>Flattens the call chain into root-to-outermost order.</summary>
    private static List<MethodCallExpression> Decompose(Expression expression)
    {
        var chain = new List<MethodCallExpression>();
        var current = expression;
        while (current is MethodCallExpression call && call.Arguments.Count > 0 && IsQueryOperator(call))
        {
            chain.Add(call);
            current = call.Arguments[0];
        }
        chain.Reverse();
        return chain;
    }

    private static bool IsQueryOperator(MethodCallExpression call) =>
        call.Method.DeclaringType == typeof(Queryable) || call.Method.DeclaringType == typeof(Enumerable);

    /// <summary>
    /// Rebuilds the operators the plan did not consume, rooted at a placeholder the executor swaps
    /// for the rows the plan produced.
    /// </summary>
    private static Expression? RebuildRemainder(
        List<MethodCallExpression> chain, int consumed, Type entityClrType)
    {
        if (consumed == chain.Count)
        {
            return null;
        }

        Expression current = BlazeDbRowsPlaceholder.For(entityClrType);
        for (var i = consumed; i < chain.Count; i++)
        {
            var call = chain[i];
            var arguments = call.Arguments.ToArray();
            arguments[0] = current;
            current = Expression.Call(call.Object, call.Method, arguments);
        }
        return current;
    }

    private static LambdaExpression Lambda(MethodCallExpression call, int index)
    {
        var argument = call.Arguments[index];
        while (argument is UnaryExpression { NodeType: ExpressionType.Quote } quote)
        {
            argument = quote.Operand;
        }
        return (LambdaExpression)argument;
    }

    private static bool TryConstantInt(Expression expression, out int value)
    {
        if (BlazeDbExpressionEvaluator.TryEvaluate(expression, out var boxed) && boxed is int i)
        {
            value = i;
            return true;
        }
        value = 0;
        return false;
    }
}
