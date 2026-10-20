using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Turns the expression <c>EF.CompileQuery</c> hands the provider into an ordinary query for one
/// execution.
/// <para>
/// EF rewrites the compiled lambda before the provider sees it: the context parameter becomes a
/// constant holding the context the query was first run against, and every other parameter becomes
/// a free parameter whose value each execution puts in its <see cref="QueryContext"/>. The compiled
/// delegate is then reused for every context with the same model. So each execution swaps that
/// first context for its own and the free parameters for the values it was given, and what is left
/// runs like a query written inline.
/// </para>
/// </summary>
internal sealed class BlazeDbCompiledQueryBinder : ExpressionVisitor
{
    private readonly DbContext _compiledFor;
    private readonly QueryContext _queryContext;
    private readonly HashSet<ParameterExpression> _bound = [];

    private BlazeDbCompiledQueryBinder(DbContext compiledFor, QueryContext queryContext)
    {
        _compiledFor = compiledFor;
        _queryContext = queryContext;
    }

    public static Expression Bind(Expression query, DbContext compiledFor, QueryContext queryContext) =>
        new BlazeDbCompiledQueryBinder(compiledFor, queryContext).Visit(query)!;

    protected override Expression VisitConstant(ConstantExpression node) =>
        ReferenceEquals(node.Value, _compiledFor) && !ReferenceEquals(_compiledFor, _queryContext.Context)
            ? Expression.Constant(_queryContext.Context, node.Type)
            : node;

    // A lambda's own parameters are the rows it is applied to, never compiled-query arguments.
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var added = node.Parameters.Where(_bound.Add).ToList();
        try
        {
            return base.VisitLambda(node);
        }
        finally
        {
            _bound.ExceptWith(added);
        }
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        !_bound.Contains(node) && node.Name is { } name && TryGetValue(name, out var value)
            ? Expression.Constant(value, node.Type)
            : node;

#if NET10_0_OR_GREATER
    protected override Expression VisitExtension(Expression node) =>
        node is QueryParameterExpression parameter && TryGetValue(parameter.Name, out var value)
            ? Expression.Constant(value, parameter.Type)
            : base.VisitExtension(node);

    private bool TryGetValue(string name, out object? value) => _queryContext.Parameters.TryGetValue(name, out value);
#else
    private bool TryGetValue(string name, out object? value) => _queryContext.ParameterValues.TryGetValue(name, out value);
#endif
}
