namespace Bit.BlazorUI;

/// <summary>Comparison operators available for column filtering.</summary>
public enum BitDataGridFilterOperator
{
    /// <summary>No operator selected. The default value; such a filter is treated as omitted/invalid.</summary>
    Unspecified = 0,
    Contains,
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
