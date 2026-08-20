namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// What the server heard in a recording the AI chat panel sent it. See <c>ChatbotController.TranscribeSpeech</c>.
/// </summary>
public class TranscribeSpeechResponseDto
{
    /// <summary>
    /// Empty rather than null when the recording carried no speech - a user who taps the microphone and says nothing
    /// gets an empty box, not an error.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// The text to read out loud. See <c>ChatbotController.SynthesizeSpeech</c>.
/// </summary>
public class SynthesizeSpeechRequestDto
{
    [Required(AllowEmptyStrings = false), StringLength(maximumLength: 8 * 1024 /*8KB*/)]
    public string Text { get; set; } = default!;
}
