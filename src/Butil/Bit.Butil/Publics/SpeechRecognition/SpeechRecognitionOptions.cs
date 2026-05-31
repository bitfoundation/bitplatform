namespace Bit.Butil;

/// <summary>Options for <see cref="SpeechRecognition.Start"/>.</summary>
public class SpeechRecognitionOptions
{
    /// <summary>BCP-47 language tag, e.g. <c>"en-US"</c>. Defaults to the document language.</summary>
    public string? Lang { get; set; }

    /// <summary>When true, recognition keeps running across pauses until <see cref="SpeechRecognition.Stop"/>.</summary>
    public bool Continuous { get; set; }

    /// <summary>When true, the engine reports interim (non-final) results too.</summary>
    public bool InterimResults { get; set; } = true;

    /// <summary>How many alternative transcripts to surface per result. 1–5 is typical.</summary>
    public int MaxAlternatives { get; set; } = 1;
}
