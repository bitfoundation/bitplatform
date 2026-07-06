namespace Bit.BlazorUI;

/// <summary>
/// Structured facts about the editor content, computed by the JS bridge and used by the
/// component to classify emptiness and drive character/word counts.
/// </summary>
public readonly record struct BitRichTextEditorContentFacts(
    bool HasText,
    bool HasEmbeddedContent,
    int CharacterCount,
    int WordCount)
{
    /// <summary>Content is empty when it has neither text nor embedded (non-text) content.</summary>
    public bool IsEmpty => !HasText && !HasEmbeddedContent;
}
