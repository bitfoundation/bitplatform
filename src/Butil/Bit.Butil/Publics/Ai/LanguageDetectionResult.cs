namespace Bit.Butil;

/// <summary>
/// One candidate language for a piece of text, from <see cref="LanguageDetectorSession.Detect"/>.
/// </summary>
public class LanguageDetectionResult
{
    /// <summary>
    /// The language, as a BCP 47 tag. The literal <c>"und"</c> means the detector could not decide.
    /// </summary>
    public string DetectedLanguage { get; set; } = string.Empty;

    /// <summary>How sure the detector is, from 0 to 1. Results come back most-confident first.</summary>
    public double Confidence { get; set; }
}
