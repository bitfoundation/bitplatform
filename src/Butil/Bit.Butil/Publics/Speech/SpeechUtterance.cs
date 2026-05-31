namespace Bit.Butil;

/// <summary>
/// Configuration for a single <see href="https://developer.mozilla.org/en-US/docs/Web/API/SpeechSynthesisUtterance">SpeechSynthesisUtterance</see>.
/// </summary>
public class SpeechUtterance
{
    /// <summary>The text to speak.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>BCP-47 language tag, e.g. <c>"en-US"</c>. <c>null</c> falls back to the document language.</summary>
    public string? Lang { get; set; }

    /// <summary>Voice name to use; must match a voice from <see cref="SpeechSynthesis.GetVoices"/>.</summary>
    public string? VoiceName { get; set; }

    /// <summary>Speech rate (0.1–10, defaults to 1).</summary>
    public double? Rate { get; set; }

    /// <summary>Pitch (0–2, defaults to 1).</summary>
    public double? Pitch { get; set; }

    /// <summary>Volume (0–1, defaults to 1).</summary>
    public double? Volume { get; set; }
}
