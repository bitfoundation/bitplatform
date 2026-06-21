namespace Bit.BlazorUI;

/// <summary>Comparison operators available for column filtering.</summary>
public enum BitDataGridFilterOperator
{
    Contains = 0,
    DoesNotContain,
    StartsWith,
    EndsWith,
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    IsEmpty,
    IsNotEmpty
}
