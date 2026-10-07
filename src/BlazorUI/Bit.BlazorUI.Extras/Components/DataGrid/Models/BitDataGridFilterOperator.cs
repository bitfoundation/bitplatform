namespace Bit.BlazorUI;

/// <summary>Comparison operators available for column filtering.</summary>
public enum BitDataGridFilterOperator
{
    /// <summary>No operator selected. The default value; such a filter is treated as omitted/invalid.</summary>
    Unspecified = 0,

    /// <summary>The text of the value contains the term, ignoring case.</summary>
    Contains,

    /// <summary>The text of the value does not contain the term, ignoring case.</summary>
    DoesNotContain,

    /// <summary>The text of the value starts with the term, ignoring case.</summary>
    StartsWith,

    /// <summary>The text of the value ends with the term, ignoring case.</summary>
    EndsWith,

    /// <summary>The value equals the operand. A null operand matches the rows whose value is null.</summary>
    Equals,

    /// <summary>The value does not equal the operand.</summary>
    NotEquals,

    /// <summary>The value is greater than the operand. A null value never matches.</summary>
    GreaterThan,

    /// <summary>The value is greater than or equal to the operand. A null value never matches.</summary>
    GreaterThanOrEqual,

    /// <summary>The value is less than the operand. A null value never matches.</summary>
    LessThan,

    /// <summary>The value is less than or equal to the operand. A null value never matches.</summary>
    LessThanOrEqual,

    /// <summary>The value is null or its text is empty. Takes no operand.</summary>
    IsEmpty,

    /// <summary>The value is neither null nor empty text. Takes no operand.</summary>
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
