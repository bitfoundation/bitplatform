using System.Linq.Expressions;

namespace Bit.BlazorUI;

internal static class ExpressionExtensions
{
    internal static string GetName<TSource, TResult>(this Expression<Func<TSource, TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return GetNameFromMemberExpression(action.Body);
    }

    private static string GetNameFromMemberExpression(Expression expression)
    {
        if (expression is MemberExpression)
        {
            return (expression as MemberExpression)!.Member.Name;
        }
        else if (expression is UnaryExpression)
        {
            return GetNameFromMemberExpression((expression as UnaryExpression)!.Operand);
        }

        throw new InvalidCastException("MemberNameUnknown.");
    }
}
