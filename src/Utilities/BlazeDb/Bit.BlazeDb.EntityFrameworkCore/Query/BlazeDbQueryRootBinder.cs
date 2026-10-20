using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Replaces every query root in a tree except the one the query is rooted at.
/// <para>
/// The root is the set reached by following the first argument of each operator down from the
/// top - the chain the translator decomposes. A root anywhere else - the inner side of a
/// <c>Join</c>, a <c>context.Orders.Any(...)</c> inside a predicate - has no plan of its own; it is
/// swapped for a constant the caller supplies, a queryable over that table's rows, so the rest of
/// the pipeline (the translator, the compiled filters, LINQ to Objects) only ever meets one root.
/// </para>
/// </summary>
internal sealed class BlazeDbQueryRootBinder : ExpressionVisitor
{
    private readonly Func<EntityQueryRootExpression, Expression> _bind;
    private bool _onSpine = true;

    private BlazeDbQueryRootBinder(Func<EntityQueryRootExpression, Expression> bind) => _bind = bind;

    public static Expression BindOtherSets(Expression query, Func<EntityQueryRootExpression, Expression> bind) =>
        new BlazeDbQueryRootBinder(bind).Visit(query)!;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (!_onSpine || node.Arguments.Count == 0)
        {
            return base.VisitMethodCall(node);
        }

        // The first argument stays on the spine; everything else hangs off it.
        var arguments = new Expression[node.Arguments.Count];
        arguments[0] = Visit(node.Arguments[0])!;
        _onSpine = false;
        for (var i = 1; i < arguments.Length; i++)
        {
            arguments[i] = Visit(node.Arguments[i])!;
        }
        var instance = Visit(node.Object);
        _onSpine = true;
        return node.Update(instance, arguments);
    }

    protected override Expression VisitExtension(Expression node) =>
        node is EntityQueryRootExpression root && !_onSpine ? _bind(root) : base.VisitExtension(node);
}
