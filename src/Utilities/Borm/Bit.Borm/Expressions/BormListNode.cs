namespace Bit.Borm;

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
