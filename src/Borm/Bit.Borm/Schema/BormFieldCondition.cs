namespace Bit.Borm;

/// <summary>
/// A single, serializable rule comparing the value at <see cref="FieldPath"/> against <see cref="Value"/>.
/// </summary>
public sealed class BormFieldCondition : IBormCondition
{
    public BormFieldCondition(string fieldPath, BormConditionOperator @operator, object? value = null)
    {
        FieldPath = fieldPath;
        Operator = @operator;
        Value = value;
    }

    public string FieldPath { get; }
    public BormConditionOperator Operator { get; }
    public object? Value { get; }

    public IEnumerable<string> Dependencies => [FieldPath];

    public bool Evaluate(IBormDataReader data)
    {
        var actual = data.GetValue(FieldPath);
        return BormConditionEvaluator.Compare(actual, Operator, Value);
    }
}
