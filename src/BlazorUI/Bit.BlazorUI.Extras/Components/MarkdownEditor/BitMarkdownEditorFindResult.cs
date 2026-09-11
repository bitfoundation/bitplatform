namespace Bit.BlazorUI;

/// <summary>
/// The outcome of a find (or find &amp; replace) round trip in the <see cref="BitMarkdownEditor"/>.
/// </summary>
/// <param name="Count">How many occurrences of the search term the document contains.</param>
/// <param name="Index">The 1-based position of the currently selected occurrence, or 0 when none is selected.</param>
public readonly record struct BitMarkdownEditorFindResult(int Count, int Index)
{
    /// <summary>
    /// A result describing a document with no occurrence of the search term.
    /// </summary>
    public static BitMarkdownEditorFindResult None { get; } = new(0, 0);

    /// <summary>
    /// True when at least one occurrence was found.
    /// </summary>
    public bool HasMatches => Count > 0;
}
