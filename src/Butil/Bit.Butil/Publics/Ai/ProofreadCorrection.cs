namespace Bit.Butil;

/// <summary>
/// One change the <see cref="Proofreader"/> made, located in the <b>original</b> input so it can be
/// highlighted in place.
/// </summary>
public class ProofreadCorrection
{
    /// <summary>Index in the original input where the corrected span starts.</summary>
    public int StartIndex { get; set; }

    /// <summary>Index in the original input where the corrected span ends, exclusive.</summary>
    public int EndIndex { get; set; }

    /// <summary>The text that replaces the span.</summary>
    public string Correction { get; set; } = string.Empty;

    /// <summary>
    /// Why the change was made - <c>"spelling"</c>, <c>"punctuation"</c>, <c>"capitalization"</c>,
    /// <c>"preposition"</c>, <c>"missing-words"</c>, <c>"grammar"</c>. Empty unless the session was
    /// created with <see cref="ProofreaderOptions.IncludeCorrectionTypes"/>.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The change in prose. Empty unless the session was created with
    /// <see cref="ProofreaderOptions.IncludeCorrectionExplanations"/>.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}
