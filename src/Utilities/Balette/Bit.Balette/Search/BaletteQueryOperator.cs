namespace Bit.Balette;

/// <summary>How a term of a query is matched.</summary>
public enum BaletteQueryOperator
{
    /// <summary>By the palette's matcher - fuzzy by default.</summary>
    Fuzzy,

    /// <summary><c>'term</c> or <c>"a phrase"</c>: the text itself, anywhere in a field.</summary>
    Exact,

    /// <summary><c>^term</c>: a field starts with it.</summary>
    Prefix,

    /// <summary><c>term$</c>: a field ends with it.</summary>
    Suffix,

    /// <summary><c>!term</c>: no field contains it.</summary>
    Negate,
}
