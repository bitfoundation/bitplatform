using System.Linq.Expressions;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// One <c>row.Member op value</c> test (or <c>values.Contains(row.Member)</c>), with the value already
/// evaluated and converted to the member's own type. C# widens the operands of a comparison before it
/// is compared - <c>row.Kind == Kind.A</c> is really <c>(int)row.Kind == 0</c> - so the boxed value in
/// the tree is often not of the property's type, and the engine's boxed index API unboxes exactly the
/// index key type. Converting here, once, means no selector rule can hand an index a value of the wrong
/// type; a value that cannot be converted losslessly yields no comparison, and the test stays a filter.
/// </summary>
internal sealed class BlazeDbComparison
{
    private BlazeDbComparison(string member, ExpressionType op, object? value, IReadOnlyList<object?>? inValues = null)
    {
        Member = member;
        Operator = op;
        Value = value;
        InValues = inValues;
    }

    public string Member { get; }

    /// <summary>The operator, or <see cref="ExpressionType.Call"/> for a membership test.</summary>
    public ExpressionType Operator { get; }

    /// <summary>The value, of the member's type; null only for a comparison against null.</summary>
    public object? Value { get; }

    /// <summary>
    /// For <c>values.Contains(row.Member)</c>: the distinct values, of the member's type. A null in
    /// the list is kept as a null: it matches the rows whose value is null, which an index over a
    /// nullable property records, and matches nothing on a property that cannot be null.
    /// </summary>
    public IReadOnlyList<object?>? InValues { get; }

    public bool IsMembership => InValues is not null;

    public bool IsRange => Operator is ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
        or ExpressionType.LessThan or ExpressionType.LessThanOrEqual;

    public static BlazeDbComparison? TryRead(Expression conjunct, ParameterExpression? parameter)
    {
        if (parameter is null)
        {
            return null;
        }
        if (conjunct is MethodCallExpression call)
        {
            return TryReadMembership(call, parameter);
        }
        if (conjunct is UnaryExpression { NodeType: ExpressionType.Not } negation)
        {
            return TryReadBooleanMember(negation.Operand, parameter, expected: false);
        }
        if (conjunct is MemberExpression)
        {
            return TryReadBooleanMember(conjunct, parameter, expected: true);
        }
        if (conjunct is not BinaryExpression binary)
        {
            return null;
        }
        var op = binary.NodeType;
        if (op is not (ExpressionType.Equal or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
            or ExpressionType.LessThan or ExpressionType.LessThanOrEqual))
        {
            return null;
        }

        if (TryMember(binary.Left, parameter, out var member, out var memberType) &&
            BlazeDbExpressionEvaluator.TryEvaluate(binary.Right, out var value))
        {
            return Create(member, op, value, memberType);
        }
        if (TryMember(binary.Right, parameter, out member, out memberType) &&
            BlazeDbExpressionEvaluator.TryEvaluate(binary.Left, out value))
        {
            return Create(member, Mirror(op), value, memberType);
        }
        return null;
    }

    private static BlazeDbComparison? Create(string member, ExpressionType op, object? value, Type memberType)
    {
        if (value is null)
        {
            return new BlazeDbComparison(member, op, null);
        }
        return BlazeDbKeyCoercion.TryCoerce(value, memberType, out var coerced) ? new BlazeDbComparison(member, op, coerced) : null;
    }

    /// <summary>
    /// <c>row.Flag</c> and <c>!row.Flag</c>, which is how a boolean predicate is actually written.
    /// They say the same thing as <c>row.Flag == true</c> and <c>== false</c>, and an index over a
    /// bool answers that in one probe - so the everyday spelling should not be the one that scans.
    /// Only a plain <c>bool</c> qualifies: a <c>bool?</c> cannot be a condition on its own, and
    /// <c>row.Flag.Value</c> is a member of a member, which is not a row member at all.
    /// </summary>
    private static BlazeDbComparison? TryReadBooleanMember(
        Expression expression, ParameterExpression parameter, bool expected) =>
        TryMember(expression, parameter, out var member, out var memberType) && memberType == typeof(bool)
            ? new BlazeDbComparison(member, ExpressionType.Equal, expected)
            : null;

    /// <summary>
    /// Reads <c>values.Contains(row.Member)</c> - the static <c>Enumerable.Contains</c> or an instance
    /// <c>Contains</c> on a list, set or array - when the values do not depend on the row. A string
    /// receiver is left alone (<c>text.Contains(row.Letter)</c> is a substring test), and so is a set
    /// with its own comparer, which may match values an index keyed on the default comparer would not.
    /// </summary>
    private static BlazeDbComparison? TryReadMembership(MethodCallExpression call, ParameterExpression parameter)
    {
        if (call.Method.Name != nameof(Enumerable.Contains))
        {
            return null;
        }
        Expression source, item;
        if (call.Object is null && call.Arguments.Count is 2 or 3)
        {
            (source, item) = (call.Arguments[0], call.Arguments[1]);
            if (call.Arguments.Count == 3 &&
                (!BlazeDbExpressionEvaluator.TryEvaluate(call.Arguments[2], out var comparer) || comparer is not null))
            {
                // An explicit comparer may match values the index would not; leave it to the filter.
                return null;
            }
        }
        else if (call.Object is not null && call.Arguments.Count == 1)
        {
            (source, item) = (call.Object, call.Arguments[0]);
        }
        else
        {
            return null;
        }

        if (!TryMember(item, parameter, out var member, out var memberType) ||
            !BlazeDbExpressionEvaluator.TryEvaluate(source, out var evaluated) ||
            evaluated is not System.Collections.IEnumerable values || evaluated is string ||
            UsesCustomComparer(evaluated))
        {
            return null;
        }

        var keys = new List<object?>();
        var seen = new HashSet<object?>();
        foreach (var value in values)
        {
            object? key = null;
            if (value is not null && !BlazeDbKeyCoercion.TryCoerce(value, memberType, out key))
            {
                return null;
            }
            if (seen.Add(key))
            {
                keys.Add(key);
            }
        }
        return new BlazeDbComparison(member, ExpressionType.Call, null, keys);
    }

    /// <summary>
    /// True when the collection carries a comparer other than the default one for its element type -
    /// a HashSet with an ignore-case comparer, a SortedSet with a custom ordering - so its Contains
    /// answers a different question than an index lookup on the raw value would.
    /// </summary>
    private static bool UsesCustomComparer(object collection)
    {
        var comparerProperty = collection.GetType().GetProperty("Comparer");
        if (comparerProperty?.GetValue(collection) is not { } comparer)
        {
            return false;
        }
        var elementType = collection.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
        if (elementType is null)
        {
            return true;
        }
        if (elementType == typeof(string) && ReferenceEquals(comparer, StringComparer.Ordinal))
        {
            return false; // Ordinal is what the default string comparers and the indexes already do.
        }
        var defaultEquality = typeof(EqualityComparer<>).MakeGenericType(elementType).GetProperty("Default")!.GetValue(null);
        var defaultOrder = typeof(Comparer<>).MakeGenericType(elementType).GetProperty("Default")!.GetValue(null);
        return !Equals(comparer, defaultEquality) && !Equals(comparer, defaultOrder);
    }

    private static bool TryMember(Expression expression, ParameterExpression parameter, out string member, out Type memberType)
    {
        var current = StripQuotes(expression);
        // Only a conversion that loses nothing may be looked through: the compiler's widening of
        // an enum or a narrow integer for the comparison, a lift to Nullable<T>, a box. A narrowing
        // cast the author wrote - (byte)row.Count == 5 - asks a different question of the member
        // than the member itself would answer (261 satisfies it too), so it stays a filter.
        while (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert &&
               IsLossless(convert.Operand.Type, convert.Type))
        {
            current = convert.Operand;
        }
        if (current is MemberExpression access && access.Expression == parameter)
        {
            member = access.Member.Name;
            memberType = access.Type;
            return true;
        }
        member = string.Empty;
        memberType = typeof(object);
        return false;
    }

    /// <summary>
    /// Whether every value of <paramref name="from"/> survives a conversion to <paramref name="to"/>
    /// unchanged: a reference or boxing conversion, a lift to <c>Nullable&lt;T&gt;</c>, an enum to its
    /// underlying type, or a numeric widening that C# performs implicitly.
    /// </summary>
    private static bool IsLossless(Type from, Type to)
    {
        if (to.IsAssignableFrom(from) || Nullable.GetUnderlyingType(to) == from)
        {
            return true;
        }
        var source = Nullable.GetUnderlyingType(from) ?? from;
        var target = Nullable.GetUnderlyingType(to) ?? to;
        if (source.IsEnum)
        {
            source = Enum.GetUnderlyingType(source);
        }
        if (target.IsEnum)
        {
            return false; // Integer to enum is the reverse of what the compiler emits; do not guess.
        }
        return source == target || Widenings.TryGetValue(source, out var wider) && wider.Contains(target);
    }

    // The implicit numeric conversions of the C# specification that are also exact - the ones the
    // compiler inserts on its own when it widens an operand for a comparison, minus those it allows
    // but that can round (int to float, long to double), since a rounded member is no longer the member.
    private static readonly Dictionary<Type, Type[]> Widenings = new()
    {
        [typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(ushort)] = [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(int)] = [typeof(long), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(long), typeof(ulong), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(decimal)],
        [typeof(ulong)] = [typeof(decimal)],
        [typeof(char)] = [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(float)] = [typeof(double)],
    };

    /// <summary>Flips a comparison written the other way round, as in <c>18 &lt;= row.Age</c>.</summary>
    private static ExpressionType Mirror(ExpressionType op) => op switch
    {
        ExpressionType.GreaterThan => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
        _ => op,
    };

    public static Expression StripQuotes(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Quote } quote)
        {
            expression = quote.Operand;
        }
        return expression;
    }
}
