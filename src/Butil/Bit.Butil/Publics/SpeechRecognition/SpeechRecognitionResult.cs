namespace Bit.Butil;

/// <summary>
/// One transcript reported by <see cref="SpeechRecognition"/>.
/// </summary>
public class SpeechRecognitionResult
{
    /// <summary>The recognized text.</summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>Engine confidence in [0, 1].</summary>
    public double Confidence { get; set; }

    /// <summary>True when the engine considers this transcript final (no more revisions).</summary>
    public bool IsFinal { get; set; }
}
