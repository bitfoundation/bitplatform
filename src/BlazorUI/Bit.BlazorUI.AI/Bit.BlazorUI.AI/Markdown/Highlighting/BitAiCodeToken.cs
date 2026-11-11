namespace Bit.BlazorUI;

/// <summary>
/// The category of a highlighted token, which maps to a CSS class.
/// </summary>
public enum BitAiTokenKind
{
    /// <summary>Text with no special meaning.</summary>
    Plain = 0,

    /// <summary>A language keyword.</summary>
    Keyword = 1,

    /// <summary>A built-in or well-known type name, or a lifetime such as Rust's <c>'a</c>.</summary>
    Type = 2,

    /// <summary>A string literal, including interpolated ones.</summary>
    String = 3,

    /// <summary>A numeric literal.</summary>
    Number = 4,

    /// <summary>A comment.</summary>
    Comment = 5,

    /// <summary>An operator.</summary>
    Operator = 6,

    /// <summary>Brackets, commas, and semicolons.</summary>
    Punctuation = 7,

    /// <summary>An identifier in call position.</summary>
    Function = 8,

    /// <summary>A property or field name, including a JSON key.</summary>
    Property = 9,

    /// <summary>A markup tag name.</summary>
    Tag = 10,

    /// <summary>A markup attribute name, or an annotation.</summary>
    Attribute = 11,

    /// <summary>A language constant such as a boolean or null literal.</summary>
    Constant = 12,

    /// <summary>An added line in a diff.</summary>
    Inserted = 13,

    /// <summary>A removed line in a diff.</summary>
    Deleted = 14,

    /// <summary>A file header or hunk marker in a diff.</summary>
    Meta = 15,

    /// <summary>A lifetime or loop label, such as Rust's <c>'a</c> or <c>'outer</c>.</summary>
    Label = 16,

    /// <summary>A variable introduced by a sigil, such as PowerShell's <c>$name</c>.</summary>
    Variable = 17,
}

/// <summary>
/// One highlighted span of source.
/// </summary>
/// <param name="Kind">The category, which selects a CSS class.</param>
/// <param name="Start">Where the span starts, in UTF-16 code units.</param>
/// <param name="Length">How long the span is.</param>
/// <remarks>
/// Tokens carry offsets rather than substrings so highlighting a large file allocates one list
/// rather than one string per token.
/// </remarks>
public readonly record struct BitAiCodeToken(BitAiTokenKind Kind, int Start, int Length)
{
    /// <summary>Extracts the token text from the source it was produced from.</summary>
    /// <param name="source">The source that was highlighted.</param>
    /// <returns>The token text.</returns>
    public ReadOnlySpan<char> Slice(string source) => source.AsSpan(Start, Length);
}
