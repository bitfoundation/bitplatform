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

    /// <summary>
    /// Returns the data path of one value the expression reads — rows included:
    /// <c>x =&gt; x.Lines[2].Quantity</c> becomes <c>Lines[2].Quantity</c>, with the index a constant or
    /// a variable the lambda captured. For reading and writing a value through the state; a schema
    /// field describes every row, so the builders take <see cref="Of"/> and refuse an index.
    /// </summary>
    /// <exception cref="ArgumentException">The expression is not a chain of property accesses and indexers.</exception>
    public static string OfValue<TModel, TValue>(Expression<Func<TModel, TValue>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var parts = new List<string>();
        var body = selector.Body;
        while (true)
        {
            switch (body)
            {
                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary:
                    body = unary.Operand;
                    continue;

                case MemberExpression { Member.Name: nameof(Nullable<int>.Value) } nullable
                    when nullable.Expression?.Type is { } owner && Nullable.GetUnderlyingType(owner) is not null:
                    body = nullable.Expression;
                    continue;

                case MemberExpression { Member: PropertyInfo property } member:
                    parts.Add(property.Name);
                    body = member.Expression!;
                    continue;

                // list[i], and anything else indexed by one integer the way a list is.
                case MethodCallExpression { Method.Name: "get_Item", Object: { } list, Arguments: [var index] }:
                    parts.Add(BormPath.Combine(string.Empty, IndexOf(index)));
                    body = list;
                    continue;

                case BinaryExpression { NodeType: ExpressionType.ArrayIndex } array:
                    parts.Add(BormPath.Combine(string.Empty, IndexOf(array.Right)));
                    body = array.Left;
                    continue;

                case ParameterExpression when parts.Count > 0:
                    parts.Reverse();
                    var sb = new System.Text.StringBuilder();
                    foreach (var part in parts)
                    {
                        if (part[0] != '[' && sb.Length > 0) sb.Append('.');
                        sb.Append(part);
                    }
                    return sb.ToString();

                default:
                    throw new ArgumentException(
                        "The selector must read a value through properties and indexers, such as x => x.Name or x => x.Lines[i].Quantity.",
                        nameof(selector));
            }
        }

        static int IndexOf(Expression index)
        {
            var value = index switch
            {
                ConstantExpression constant => constant.Value,
                // A captured local: a field of the closure the compiler generated.
                MemberExpression { Expression: ConstantExpression closure, Member: FieldInfo field } => field.GetValue(closure.Value),
                _ => Expression.Lambda(index).Compile(preferInterpretation: true).DynamicInvoke()
            };
            return value is int i && i >= 0
                ? i
                : throw new ArgumentException($"An index in the selector is not a non-negative integer ({value ?? "null"}).");
        }
    }
}
