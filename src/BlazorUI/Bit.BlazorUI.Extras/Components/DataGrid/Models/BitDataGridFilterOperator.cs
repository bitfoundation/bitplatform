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
    IsNotEmpty,

    /// <summary>
    /// The value equals any member of a set. <see cref="BitDataGridFilterDescriptor.Value"/> is a
    /// collection (any <see cref="System.Collections.IEnumerable"/> other than a string); an empty set
    /// carries no criteria and applies no filter. A <c>null</c> member matches rows whose value is null.
    /// </summary>
    In,

    /// <summary>The value equals no member of a set - the negation of <see cref="In"/>.</summary>
    NotIn
}
