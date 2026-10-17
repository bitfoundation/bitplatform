using System.Linq.Expressions;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

// IQueryCompiler is EF's own extension point for replacing the query pipeline, but it lives behind
// the internal-API warning because EF reserves the right to change it. This provider accepts that:
// implementing it is what lets a query stay a few expression walks instead of a translation stack.
#pragma warning disable EF1001

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Replaces EF's query pipeline outright.
///
/// That pipeline exists to turn a LINQ tree into another language and to rebuild objects from the
/// flat rows that language returns. BlazeDb needs neither: the data is already .NET objects in
/// memory, so a query is a plan over them plus, where the plan does not reach, ordinary LINQ. What
/// is left is small enough to be read in one sitting, which is the point.
/// </summary>
internal sealed class BlazeDbQueryCompiler : IQueryCompiler
{
    private readonly BlazeDbQueryExecutor _executor;
    private readonly ICurrentDbContext _currentContext;

    public BlazeDbQueryCompiler(IBlazeDbTableCache tables, ICurrentDbContext currentContext)
    {
        _executor = new BlazeDbQueryExecutor(tables);
        _currentContext = currentContext;
    }

    private DbContext Context => _currentContext.Context;

    public TResult Execute<TResult>(Expression query) => (TResult)_executor.Execute(query, Context)!;

    public TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken = default)
    {
        // EF asks for one of two shapes: a Task<T> for the operators that end a query, such as
        // FirstOrDefaultAsync, or an IAsyncEnumerable<T> for the ones that stream it.
        var resultType = typeof(TResult);
        if (resultType.IsGenericType)
        {
            var definition = resultType.GetGenericTypeDefinition();
            var element = resultType.GetGenericArguments()[0];

            if (definition == typeof(IAsyncEnumerable<>))
            {
                return (TResult)StreamMethod
                    .MakeGenericMethod(element)
                    .Invoke(this, [query, cancellationToken])!;
            }
            if (definition == typeof(Task<>))
            {
                return (TResult)CompleteMethod
                    .MakeGenericMethod(element)
                    .Invoke(this, [query, cancellationToken])!;
            }
        }

        return Execute<TResult>(query);
    }

    private IAsyncEnumerable<T> Stream<T>(Expression query, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync<T>(query, Context, cancellationToken);

    private Task<T> Complete<T>(Expression query, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }
        try
        {
            return Task.FromResult((T)_executor.Execute(query, Context)!);
        }
        catch (Exception ex)
        {
            return Task.FromException<T>(ex);
        }
    }

    public Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query) =>
        _ => Execute<TResult>(query);

    public Func<QueryContext, TResult> CreateCompiledAsyncQuery<TResult>(Expression query) =>
        _ => ExecuteAsync<TResult>(query);

    /// <summary>
    /// Ahead-of-time query precompilation emits the code EF's pipeline would have generated, and
    /// there is no such code here - translation is a handful of expression walks that run in
    /// microseconds, so there is nothing to save by doing it at build time.
    /// </summary>
    public Expression<Func<QueryContext, TResult>> PrecompileQuery<TResult>(Expression query, bool async) =>
        throw new NotSupportedException(
            "BlazeDb does not support precompiled queries; it translates queries directly at execution time.");

    private static readonly System.Reflection.MethodInfo StreamMethod =
        typeof(BlazeDbQueryCompiler).GetMethod(nameof(Stream), Instance)!;

    private static readonly System.Reflection.MethodInfo CompleteMethod =
        typeof(BlazeDbQueryCompiler).GetMethod(nameof(Complete), Instance)!;

    private const System.Reflection.BindingFlags Instance =
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
}
