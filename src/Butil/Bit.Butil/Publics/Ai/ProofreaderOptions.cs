namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="Proofreader"/> session.
/// </summary>
public class ProofreaderOptions
{
    /// <summary>
    /// Ask the model to label each correction with why it was made (spelling, punctuation, grammar,
    /// and so on). Off by default, because labelling costs the model extra work.
    /// </summary>
    public bool? IncludeCorrectionTypes { get; set; }

    /// <summary>
    /// Ask the model to explain each correction in prose. Off by default, for the same reason as
    /// <see cref="IncludeCorrectionTypes"/>.
    /// </summary>
    public bool? IncludeCorrectionExplanations { get; set; }

    /// <summary>The languages the input will be in, as BCP 47 tags.</summary>
    public string[]? ExpectedInputLanguages { get; set; }
}
