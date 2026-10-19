using System.Linq.Expressions;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Splits the collected <c>Where</c> bodies into individual AND-ed tests and puts the survivors
/// back together once index selection has claimed the ones it can serve.
/// </summary>
internal static class BlazeDbConjuncts
{
    public static List<Expression> Flatten(List<LambdaExpression> predicates, out ParameterExpression? parameter)
    {
        parameter = predicates.Count > 0 ? predicates[0].Parameters[0] : null;
        var result = new List<Expression>();
        foreach (var predicate in predicates)
        {
            // Separate Where calls each declare their own parameter; rewriting them to a single one
            // lets the surviving tests be recombined into one lambda.
            var body = ReferenceEquals(predicate.Parameters[0], parameter)
                ? predicate.Body
                : new ParameterRewriter(predicate.Parameters[0], parameter!).Visit(predicate.Body)!;
            Split(body, result);
        }
        return result;
    }

    public static IReadOnlyList<LambdaExpression> Rebuild(
        List<Expression> conjuncts, HashSet<int> used, ParameterExpression? parameter, List<LambdaExpression> original)
    {
        if (used.Count == 0)
        {
            return original;
        }
        if (used.Count == conjuncts.Count || parameter is null)
        {
            return [];
        }

        Expression? body = null;
        for (var i = 0; i < conjuncts.Count; i++)
        {
            if (used.Contains(i))
            {
                continue;
            }
            body = body is null ? conjuncts[i] : Expression.AndAlso(body, conjuncts[i]);
        }
        return body is null ? [] : [Expression.Lambda(body, parameter)];
    }

    private static void Split(Expression expression, List<Expression> into)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso } and)
        {
            Split(and.Left, into);
            Split(and.Right, into);
            return;
        }
        into.Add(expression);
    }

    private sealed class ParameterRewriter : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterRewriter(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : node;
    }
}
