namespace Bit.Borm;

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
