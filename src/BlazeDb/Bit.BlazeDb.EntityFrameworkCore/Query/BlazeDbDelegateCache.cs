using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Compiles the lambdas a plan runs - residual predicates and ordering key selectors - once per
/// query shape rather than once per execution.
/// <para>
/// Replacing EF's query pipeline also gave up its compiled-query cache, and <c>Compile()</c> is
/// the one expensive step left in translating a query: on a JIT it costs more than the whole plan,
/// and on WebAssembly it means building an interpreter closure every time. Two executions of the
/// same query differ only in the values captured in the tree - <c>min</c> in <c>p =&gt; p.Age &gt; min</c>
/// is a field read off a fresh closure object each call - so the lambda is rewritten with every
/// constant replaced by a slot in a values array and compiled into a factory,
/// <c>values =&gt; (row =&gt; ...)</c>. The rewritten lambda is the cache key: it is identical for every
/// execution of the same query, compared structurally with EF's own expression comparer. Executing
/// then costs one array allocation and one closure, with no compilation at all.
/// </para>
/// </summary>
internal static class BlazeDbDelegateCache
{
    private const int MaxEntries = 1024;

    private static readonly ConcurrentDictionary<LambdaExpression, Func<object?[], Delegate>> Factories =
        new(ExpressionEqualityComparer.Instance);

    private static readonly ConcurrentDictionary<(string Name, Type Element, Type Key), MethodInfo> Operators = new();

    /// <summary>The compiled delegate for <paramref name="lambda"/>, with its captured values bound.</summary>
    public static Delegate Compile(LambdaExpression lambda)
    {
        var parameterizer = new Parameterizer();
        var template = (LambdaExpression)parameterizer.Visit(lambda);
        var values = parameterizer.Values;

        if (!Factories.TryGetValue(template, out var factory))
        {
            if (Factories.Count >= MaxEntries)
            {
                // A query generator producing endless distinct shapes must not grow this without
                // bound; dropping everything is crude but rare and only costs the next compiles.
                Factories.Clear();
            }
            factory = BuildFactory(template, parameterizer.ValuesParameter);
            Factories[template] = factory;
        }
        return factory(values.ToArray());
    }

    /// <summary>The <see cref="Enumerable"/> counterpart of a <see cref="Queryable"/> ordering operator, looked up once.</summary>
    public static MethodInfo EnumerableOperator(string name, Type[] genericArguments) =>
        Operators.GetOrAdd((name, genericArguments[0], genericArguments[1]), static key =>
            typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == key.Name && m.GetParameters().Length == 2 && m.IsGenericMethodDefinition)
                .MakeGenericMethod(key.Element, key.Key));

    private static Func<object?[], Delegate> BuildFactory(LambdaExpression template, ParameterExpression valuesParameter)
    {
        var factoryType = typeof(Func<,>).MakeGenericType(typeof(object?[]), template.Type);
        var factory = Expression.Lambda(factoryType, template, valuesParameter).Compile();
        // Func<T, TResult> is covariant in TResult and every delegate type derives from Delegate,
        // so the typed factory converts to the general one without a wrapper.
        return (Func<object?[], Delegate>)factory;
    }

    /// <summary>
    /// Lifts every constant out of a lambda into a values array, leaving a typed read of the array
    /// in its place. Constants are what differ between executions of one query - closure objects
    /// above all, but literals as well, since two call sites of the same shape then share one
    /// compiled delegate too.
    /// </summary>
    private sealed class Parameterizer : ExpressionVisitor
    {
        public readonly ParameterExpression ValuesParameter = Expression.Parameter(typeof(object?[]), "__values");
        public readonly List<object?> Values = [];

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var index = Values.Count;
            Values.Add(node.Value);
            return Expression.Convert(
                Expression.ArrayIndex(ValuesParameter, Expression.Constant(index)), node.Type);
        }
    }
}
