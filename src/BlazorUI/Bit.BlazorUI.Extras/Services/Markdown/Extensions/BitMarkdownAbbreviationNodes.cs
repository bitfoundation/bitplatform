namespace Bit.BlazorUI;

/// <summary>
/// An abbreviation definition (<c>*[HTML]: HyperText Markup Language</c>). Like a link reference
/// definition it declares something rather than saying it, so it renders nothing;
/// <see cref="BitMarkdownAbbreviationAstProcessor"/> removes it and expands the term wherever the
/// document uses it.
/// </summary>
public sealed class BitMarkdownAbbreviationDefinitionNode : BitMarkdownNode
{
    /// <summary>The abbreviated term, matched in the text exactly as written here.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>What the term stands for, which becomes the <c>title</c> of every expansion.</summary>
    public string Title { get; init; } = string.Empty;
}

/// <summary>An expanded abbreviation, rendered as <c>&lt;abbr title="..."&gt;</c>.</summary>
public sealed class BitMarkdownAbbreviationNode : BitMarkdownNode
{
    /// <summary>The term as it appears in the text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>What it stands for.</summary>
    public string Title { get; init; } = string.Empty;
}
