using System;
using System.Linq.Expressions;

namespace Bit.BlazorUI;

public static class ExpressionExtensions
{
    public static string GetName<TSource, TResult>(this Expression<Func<TSource, TResult>> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));


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
