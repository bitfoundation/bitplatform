namespace Bit.Borm;

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
