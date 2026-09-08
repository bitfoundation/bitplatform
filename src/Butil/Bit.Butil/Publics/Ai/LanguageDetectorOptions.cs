namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="LanguageDetector"/> session.
/// </summary>
public class LanguageDetectorOptions
{
    /// <summary>
    /// The languages the input is expected to be in, as BCP 47 tags. Optional - the detector works
    /// without it, and declaring them only lets the runtime refuse a set it can't serve up front.
    /// </summary>
    public string[]? ExpectedInputLanguages { get; set; }
}
