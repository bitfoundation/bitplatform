using System.Linq.Expressions;
using System.Reflection;

namespace Bit.Borm;

/// <summary>
/// Turns a member-access lambda into the dotted data path Borm binds by:
/// <c>x =&gt; x.Address.City</c> becomes <c>Address.City</c>. Shared by the typed builder, the step
/// builder and anywhere else a refactor-safe field reference is useful.
/// </summary>
public static class BormExpressionPath
{
    /// <summary>Returns the dotted path of the member the expression selects.</summary>
    /// <exception cref="ArgumentException">The expression is not a chain of property accesses.</exception>
    public static string Of<TModel, TValue>(Expression<Func<TModel, TValue>> selector)
        => Resolve(selector).Path;

    /// <summary>Returns the dotted path and the member it ends at.</summary>
    public static (string Path, MemberInfo Member) Resolve<TModel, TValue>(Expression<Func<TModel, TValue>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        Expression body = selector.Body;
        var parts = new List<string>();
        MemberInfo? leaf = null;

        while (true)
        {
            // `x => x.Age` typed as Func<T, object?> arrives wrapped in a boxing conversion.
            if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                body = unary.Operand;
                continue;
            }

            if (body is MemberExpression member)
            {
                // `x => x.Age!.Value` on an int? is the property Age, read through Nullable<T>; the
                // form binds Age itself, and "Age.Value" names nothing the accessor can find.
                if (member.Member.Name == nameof(Nullable<int>.Value)
                    && member.Expression?.Type is { } owner && Nullable.GetUnderlyingType(owner) is not null)
                {
                    body = member.Expression;
                    continue;
                }

                // Data is read and written through properties only, so a field would produce a path
                // that silently binds to nothing. Refuse it here, where the mistake is visible.
                if (member.Member is not PropertyInfo)
                    throw new ArgumentException(
                        $"Field selector must select properties; '{member.Member.Name}' is a field. " +
                        "Expose it as a property to bind it.", nameof(selector));

                leaf ??= member.Member;
                parts.Add(member.Member.Name);
                body = member.Expression!;
                continue;
            }

            if (body is ParameterExpression) break;

            throw new ArgumentException(
                "Field selector must be a property access such as x => x.Name or x => x.Address.City.",
                nameof(selector));
        }

        if (leaf is null || parts.Count == 0)
            throw new ArgumentException("Field selector must select a property.", nameof(selector));

        parts.Reverse();
        return (string.Join('.', parts), leaf);
    }
}
