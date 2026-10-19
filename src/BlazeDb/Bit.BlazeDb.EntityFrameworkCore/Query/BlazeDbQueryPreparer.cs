using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Cleans a query expression up before translation: reads off the tracking operators and removes
/// them, rewrites EF's own property accessor into a plain member access, and rejects the operators
/// that describe a shape the engine has no concept of.
/// </summary>
internal static class BlazeDbQueryPreparer
{
    public static Expression Prepare(Expression query, out bool? tracking)
    {
        var stripper = new TrackingStripper();
        var prepared = stripper.Visit(query)!;
        tracking = stripper.Tracking;
        return prepared;
    }

    /// <summary>
    /// Rewrites two shapes into the plain form the translator (and LINQ to Objects) understand.
    /// <para>
    /// <c>EF.Property&lt;T&gt;(row, "Name")</c> is how EF itself refers to a property in the trees it
    /// builds - <c>Find</c> is the everyday case - and how an application reaches a shadow property.
    /// The engine has no shadow properties, so the call becomes the member access it stands for:
    /// that lets the translator match it against an index and lets a residual filter compile (the
    /// method itself throws when it is ever executed).
    /// </para>
    /// <para>
    /// <c>values.Contains(row.Member)</c> on an array binds, since C# 14, to the span-based
    /// <c>MemoryExtensions.Contains</c> with an implicit array-to-span conversion in front of it,
    /// even inside an expression tree - with or without a comparer argument, depending on the
    /// element type. It is put back to <c>Enumerable.Contains</c> over the array, which is what it
    /// means and what can be evaluated.
    /// </para>
    /// </summary>
    private class Normalizer : ExpressionVisitor
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type Type, string Name), MemberInfo?> Members = new();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(EF) &&
                node.Method.Name == nameof(EF.Property) &&
                node.Arguments is [_, ConstantExpression { Value: string name }])
            {
                var target = Visit(node.Arguments[0])!;
                var member = Members.GetOrAdd((target.Type, name), static k => k.Type.GetProperty(k.Name) as MemberInfo ?? k.Type.GetField(k.Name));
                if (member is not null)
                {
                    Expression access = Expression.MakeMemberAccess(target, member);
                    return access.Type == node.Type ? access : Expression.Convert(access, node.Type);
                }
            }

            if (node.Method.DeclaringType == typeof(MemoryExtensions) &&
                node.Method.Name == nameof(MemoryExtensions.Contains) &&
                node.Method.IsGenericMethod &&
                node.Arguments.Count is 2 or 3 &&
                UnwrapSpanConversion(node.Arguments[0]) is { } source &&
                source.Type.IsArray)
            {
                // The three-argument form carries a comparer, which the compiler fills with null for
                // element types that are not IEquatable<T> (a Nullable<T>, for one). Enumerable has
                // the same two shapes; a null comparer is dropped so the call reads as the plain one.
                var elementType = node.Method.GetGenericArguments()[0];
                var arguments = new List<Expression> { Visit(source), Visit(node.Arguments[1]) };
                if (node.Arguments.Count == 3 && node.Arguments[2] is not ConstantExpression { Value: null })
                {
                    arguments.Add(Visit(node.Arguments[2]));
                }
                return Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [elementType], arguments.ToArray());
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>The array behind an implicit array-to-span conversion, or null when it is something else.</summary>
        private static Expression? UnwrapSpanConversion(Expression expression) => expression switch
        {
            MethodCallExpression { Method.Name: "op_Implicit", Arguments: [var inner] } => inner,
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: var inner } => inner,
            _ => null,
        };
    }

    public static EntityQueryRootExpression FindRoot(Expression query)
    {
        var current = query;
        while (current is MethodCallExpression { Arguments.Count: > 0 } call)
        {
            current = call.Arguments[0];
        }
        return current as EntityQueryRootExpression
            ?? throw new NotSupportedException(
                "BlazeDb can only run queries that start from a DbSet. Queries spanning several " +
                "sets, or rooted at a local collection, are not supported.");
    }

    private sealed class TrackingStripper : Normalizer
    {
        public bool? Tracking { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions))
            {
                switch (node.Method.Name)
                {
                    case nameof(EntityFrameworkQueryableExtensions.AsTracking):
                        Tracking = true;
                        return Visit(node.Arguments[0]);
                    case nameof(EntityFrameworkQueryableExtensions.AsNoTracking):
                    case nameof(EntityFrameworkQueryableExtensions.AsNoTrackingWithIdentityResolution):
                        Tracking = false;
                        return Visit(node.Arguments[0]);
                    case nameof(EntityFrameworkQueryableExtensions.Include):
                    case nameof(EntityFrameworkQueryableExtensions.ThenInclude):
                        throw new NotSupportedException(
                            "BlazeDb rows have no navigations, so Include has nothing to load. " +
                            "Model relationships as key properties and query the other table directly.");
                    case nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters):
                    case nameof(EntityFrameworkQueryableExtensions.TagWith):
                        return Visit(node.Arguments[0]);
                }
            }
            return base.VisitMethodCall(node);
        }
    }
}
