namespace Bit.Borm;

/// <summary>How the operands of a <see cref="BormFieldCondition"/> are compared.</summary>
public enum BormConditionOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    NotContains,
    In,
    NotIn,
    IsEmpty,
    IsNotEmpty,
    IsTrue,
    IsFalse,
    Matches,
    StartsWith,
    EndsWith
}
