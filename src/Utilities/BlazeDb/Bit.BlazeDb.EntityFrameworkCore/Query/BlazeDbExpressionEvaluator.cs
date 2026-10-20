using System.Linq.Expressions;
using System.Reflection;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Evaluates the closed-over parts of a query - a captured variable, a constant, a field of a
/// closure. LINQ trees carry these as member accesses over a display class, and reading them
/// directly avoids compiling an expression just to learn a value. Anything more elaborate that
/// still does not depend on the row (<c>DateTime.UtcNow.AddDays(-7)</c>, <c>ids.Count</c>) is
/// compiled and run once, so it can still drive an index lookup instead of forcing a scan.
/// </summary>
internal static class BlazeDbExpressionEvaluator
{
    public static bool TryEvaluate(Expression expression, out object? value)
    {
        switch (expression)
        {
            case ConstantExpression constant:
                value = constant.Value;
                return true;

            case MemberExpression member:
            {
                if (member.Expression is null)
                {
                    return TryRead(member.Member, null, out value);
                }
                if (TryEvaluate(member.Expression, out var target) && target is not null)
                {
                    return TryRead(member.Member, target, out value);
                }
                value = null;
                return false;
            }

            case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
                when convert.Type.IsAssignableFrom(convert.Operand.Type)
                     || Nullable.GetUnderlyingType(convert.Type) == convert.Operand.Type:
                return TryEvaluate(convert.Operand, out value);

            case ParameterExpression:
                value = null;
                return false;

            default:
                return TryCompile(expression, out value);
        }
    }

    private static bool TryCompile(Expression expression, out object? value)
    {
        value = null;
        if (!RowIndependence.Check(expression))
        {
            return false;
        }
        try
        {
            // Interpreted: this runs once per translation, so a JIT compile would cost more than it saves.
            value = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile(preferInterpretation: true).Invoke();
            return true;
        }
        catch
        {
            // Whatever it was, the predicate still runs as a residual filter and gives the right
            // answer; only the chance of an index lookup is lost.
            return false;
        }
    }

    /// <summary>
    /// A subtree can be evaluated ahead of the rows only if it neither reads a lambda parameter nor
    /// contains a query root or another provider-specific node that has no value on its own.
    /// </summary>
    private sealed class RowIndependence : ExpressionVisitor
    {
        private bool _independent = true;

        public static bool Check(Expression expression)
        {
            var visitor = new RowIndependence();
            visitor.Visit(expression);
            return visitor._independent;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _independent = false;
            return node;
        }

        protected override Expression VisitExtension(Expression node)
        {
            _independent = false;
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            // A nested lambda's own parameters are fine; only free parameters matter. Treating any
            // lambda as dependent is the conservative choice and costs nothing but an index.
            _independent = false;
            return node;
        }
    }

    private static bool TryRead(MemberInfo member, object? target, out object? value)
    {
        switch (member)
        {
            case FieldInfo field:
                value = field.GetValue(target);
                return true;
            case PropertyInfo { CanRead: true } property when property.GetIndexParameters().Length == 0:
                value = property.GetValue(target);
                return true;
            default:
                value = null;
                return false;
        }
    }
}
