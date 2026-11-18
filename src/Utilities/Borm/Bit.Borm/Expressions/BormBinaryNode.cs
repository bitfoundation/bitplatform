namespace Bit.Borm;

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
