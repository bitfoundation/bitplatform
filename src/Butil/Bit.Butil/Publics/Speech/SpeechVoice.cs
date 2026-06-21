namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/SpeechSynthesisVoice">SpeechSynthesisVoice</see>.
/// </summary>
public class SpeechVoice
{
    public string Name { get; set; } = string.Empty;

    /// <summary>BCP-47 language tag - e.g. <c>"en-US"</c>.</summary>
    public string Lang { get; set; } = string.Empty;

    /// <summary>Voice URI (often the same as <see cref="Name"/> for local voices).</summary>
    public string VoiceUri { get; set; } = string.Empty;

    /// <summary>True for the runtime's default voice for the chosen language.</summary>
    public bool Default { get; set; }

    /// <summary>True when synthesis happens locally (no server round-trip).</summary>
    public bool LocalService { get; set; }
}
