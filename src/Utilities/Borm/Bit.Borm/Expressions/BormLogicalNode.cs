namespace Bit.Borm;

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
