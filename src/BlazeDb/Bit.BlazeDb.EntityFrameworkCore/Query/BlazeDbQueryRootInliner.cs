using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Replaces every reference to an EF queryable - <c>context.Orders</c> captured by a lambda,
/// <c>context.Set&lt;Order&gt;()</c>, a <c>DbSet</c> a compiled query reaches through its context -
/// with the query expression it stands for, which is how EF's own pipeline sees them too.
/// <para>
/// Left as they are, such references are opaque values, not query roots: the root binder cannot bind
/// them, and the LINQ to Objects that runs whatever the plan does not absorb would call back into the
/// provider through each of them - once per outer row for a subquery in a predicate. Inlined, they
/// are roots like any other.
/// </para>
/// </summary>
internal sealed class BlazeDbQueryRootInliner : ExpressionVisitor
{
    private static readonly BlazeDbQueryRootInliner Instance = new();

    public static Expression Inline(Expression query) => Instance.Visit(query)!;

    protected override Expression VisitMember(MemberExpression node) =>
        TryInline(node, out var inlined) ? inlined : base.VisitMember(node);

    protected override Expression VisitMethodCall(MethodCallExpression node) =>
        node.Method.DeclaringType == typeof(DbContext) && node.Method.Name == nameof(DbContext.Set) &&
        TryInline(node, out var inlined)
            ? inlined
            : base.VisitMethodCall(node);

    protected override Expression VisitConstant(ConstantExpression node) =>
        TryInline(node, out var inlined) ? inlined : node;

    /// <summary>
    /// Inlines <paramref name="node"/> when it does not depend on the row and evaluates to a
    /// queryable of EF's own - never to any other provider's, whose expression may well be a
    /// constant holding the queryable itself.
    /// </summary>
    private bool TryInline(Expression node, out Expression inlined)
    {
        inlined = node;
        if (!typeof(IQueryable).IsAssignableFrom(node.Type) ||
            !BlazeDbExpressionEvaluator.TryEvaluate(node, out var value) ||
            value is not IQueryable { Provider: IAsyncQueryProvider } queryable ||
            queryable.Expression is ConstantExpression { Value: var self } && ReferenceEquals(self, queryable))
        {
            return false;
        }
        inlined = Visit(queryable.Expression)!;
        return true;
    }
}
