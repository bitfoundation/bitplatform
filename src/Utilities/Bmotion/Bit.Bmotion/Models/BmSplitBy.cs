namespace Bit.Bmotion;

/// <summary>How <see cref="BmotionSplitText"/> breaks its text into independently animated units.</summary>
public enum BmSplitBy
{
    /// <summary>
    /// One unit per user-perceived character (grapheme cluster, so emoji and combining marks stay
    /// intact). Characters are grouped into words so a word never breaks across two lines.
    /// </summary>
    Chars,

    /// <summary>One unit per whitespace-delimited word.</summary>
    Words,

    /// <summary>
    /// One unit per authored line - the text is split on newline characters. (Visual line
    /// detection would require measuring the laid-out text; authored lines are deterministic and
    /// survive re-flow, so they are what Bmotion splits on.)
    /// </summary>
    Lines,
}
