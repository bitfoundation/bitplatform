namespace Bit.Borm;

internal sealed class BormLiteralNode(object? value) : BormExpressionNode
{
    public object? Value { get; } = value;

    public override object? Evaluate(IBormDataReader data) => Value;
}
