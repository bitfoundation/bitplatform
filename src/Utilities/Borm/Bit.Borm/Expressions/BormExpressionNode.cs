using System.Collections;

namespace Bit.Borm;

/// <summary>One node of a parsed <see cref="BormExpression"/>.</summary>
internal abstract class BormExpressionNode
{
    /// <summary>Evaluates the node. The result may be <see cref="BormExpressionValue.Unknown"/>.</summary>
    public abstract object? Evaluate(IBormDataReader data);

    /// <summary>Adds every field path this node and its children read to <paramref name="paths"/>.</summary>
    public virtual void CollectPaths(List<string> paths) { }
}

internal sealed class BormLiteralNode(object? value) : BormExpressionNode
{
    public object? Value { get; } = value;

    public override object? Evaluate(IBormDataReader data) => Value;
}

internal sealed class BormListNode(IReadOnlyList<BormExpressionNode> items) : BormExpressionNode
{
    public override object? Evaluate(IBormDataReader data)
    {
        var list = new List<object?>(items.Count);
        foreach (var item in items) list.Add(BormExpressionValue.Collapse(item.Evaluate(data)));
        return list;
    }

    public override void CollectPaths(List<string> paths)
    {
        foreach (var item in items) item.CollectPaths(paths);
    }
}

/// <summary>
/// A reference to a field: <c>{Address.City}</c>, or with <c>[]</c> standing for every row of a list,
/// <c>{Lines[].Total}</c>, which reads as the list of every row's value.
/// </summary>
internal sealed class BormFieldNode(string path) : BormExpressionNode
{
    public string Path { get; } = path;

    private readonly int _wildcard = path.IndexOf("[]", StringComparison.Ordinal);

    public override object? Evaluate(IBormDataReader data)
        => _wildcard < 0 ? data.GetValue(Path) : Expand(data, Path);

    public override void CollectPaths(List<string> paths) => paths.Add(Path);

    /// <summary>
    /// Reads every row of the list before the first <c>[]</c>, continuing with the rest of the path in
    /// each — itself possibly another <c>[]</c>, so a list of lists flattens into one list of values.
    /// The list is found the way any reference is, relative to the owning object first; the rows are
    /// then read at the absolute path it was found at.
    /// </summary>
    private static List<object?> Expand(IBormDataReader data, string path)
    {
        var results = new List<object?>();
        var wildcard = path.IndexOf("[]", StringComparison.Ordinal);
        var (store, listPath) = BormScopedDataReader.Resolve(data, path[..wildcard]);
        Walk(store, listPath, path[(wildcard + 2)..], results);
        return results;

        static void Walk(IBormDataReader store, string listPath, string rest, List<object?> into)
        {
            var count = store.GetValue(listPath) switch
            {
                ICollection c => c.Count,
                IEnumerable e and not string => e.Cast<object?>().Count(),
                _ => 0
            };

            var next = rest.IndexOf("[]", StringComparison.Ordinal);
            for (var i = 0; i < count; i++)
            {
                var item = BormPath.Combine(listPath, i);
                if (next < 0)
                {
                    into.Add(store.GetValue(item + rest));
                    continue;
                }
                Walk(store, item + rest[..next], rest[(next + 2)..], into);
            }
        }
    }
}

internal enum BormUnaryOperator { Negate, Plus, Not }

internal sealed class BormUnaryNode(BormUnaryOperator op, BormExpressionNode operand) : BormExpressionNode
{
    public override object? Evaluate(IBormDataReader data)
    {
        var value = operand.Evaluate(data);
        return op switch
        {
            BormUnaryOperator.Negate => BormExpressionValue.Negate(BormExpressionValue.Collapse(value)),
            BormUnaryOperator.Plus => BormExpressionValue.AsNumber(BormExpressionValue.Collapse(value)),
            _ => BormExpressionValue.ToBoolean(value) is { } b ? !b : BormExpressionValue.Unknown
        };
    }

    public override void CollectPaths(List<string> paths) => operand.CollectPaths(paths);
}

internal enum BormBinaryOperator
{
    Add, Subtract, Multiply, Divide, Modulo,
    Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual,
    In, NotIn
}

internal sealed class BormBinaryNode(BormBinaryOperator op, BormExpressionNode left, BormExpressionNode right) : BormExpressionNode
{
    public override object? Evaluate(IBormDataReader data)
    {
        var a = BormExpressionValue.Collapse(left.Evaluate(data));
        var b = BormExpressionValue.Collapse(right.Evaluate(data));
        return op switch
        {
            BormBinaryOperator.Add => BormExpressionValue.Add(a, b),
            BormBinaryOperator.Subtract => BormExpressionValue.Subtract(a, b),
            BormBinaryOperator.Multiply => BormExpressionValue.Arithmetic('*', a, b),
            BormBinaryOperator.Divide => BormExpressionValue.Arithmetic('/', a, b),
            BormBinaryOperator.Modulo => BormExpressionValue.Arithmetic('%', a, b),
            // Equality treats a missing value as a value of its own, as BormFieldCondition does, so
            // `{Notes} = null` asks whether it is empty and `{Kind} != 'x'` holds while Kind is unset.
            BormBinaryOperator.Equal => BormExpressionValue.Equal(a, b),
            BormBinaryOperator.NotEqual => !BormExpressionValue.Equal(a, b),
            BormBinaryOperator.Less => BormExpressionValue.Compare(a, b, static c => c < 0),
            BormBinaryOperator.LessOrEqual => BormExpressionValue.Compare(a, b, static c => c <= 0),
            BormBinaryOperator.Greater => BormExpressionValue.Compare(a, b, static c => c > 0),
            BormBinaryOperator.GreaterOrEqual => BormExpressionValue.Compare(a, b, static c => c >= 0),
            BormBinaryOperator.In => BormConditionEvaluator.In(a, b),
            BormBinaryOperator.NotIn => !BormConditionEvaluator.In(a, b),
            _ => null
        };
    }

    public override void CollectPaths(List<string> paths)
    {
        left.CollectPaths(paths);
        right.CollectPaths(paths);
    }
}

/// <summary>
/// <c>and</c> / <c>or</c>, in three-valued logic and short-circuited: the right side is not evaluated
/// once the left side has decided the answer.
/// </summary>
internal sealed class BormLogicalNode(bool isAnd, BormExpressionNode left, BormExpressionNode right) : BormExpressionNode
{
    public override object? Evaluate(IBormDataReader data)
    {
        var a = BormExpressionValue.ToBoolean(left.Evaluate(data));
        if (isAnd && a == false) return false;
        if (!isAnd && a == true) return true;

        var b = BormExpressionValue.ToBoolean(right.Evaluate(data));
        if (isAnd) return b == false ? false : a == true && b == true ? true : BormExpressionValue.Unknown;
        return b == true ? true : a == false && b == false ? false : BormExpressionValue.Unknown;
    }

    public override void CollectPaths(List<string> paths)
    {
        left.CollectPaths(paths);
        right.CollectPaths(paths);
    }
}

internal sealed class BormCallNode(BormExpressionFunction function, IReadOnlyList<BormExpressionNode> args) : BormExpressionNode
{
    public override object? Evaluate(IBormDataReader data)
    {
        if (function.Lazy is { } lazy) return lazy(args, data);

        var values = new object?[args.Count];
        for (var i = 0; i < args.Count; i++) values[i] = BormExpressionValue.Collapse(args[i].Evaluate(data));
        return function.Body!(values);
    }

    public override void CollectPaths(List<string> paths)
    {
        foreach (var arg in args) arg.CollectPaths(paths);
    }
}
