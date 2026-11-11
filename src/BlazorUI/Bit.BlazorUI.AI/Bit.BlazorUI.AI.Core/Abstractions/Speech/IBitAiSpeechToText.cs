namespace Bit.BlazorUI;

/// <summary>
/// Turns recorded speech into text: the app's own speech-to-text service (Whisper, Azure Speech, a model of its own ...)
/// that <c>BitAiVoiceInput</c> dictates through, in place of the speech API of the browser.
/// </summary>
/// <remarks>
/// <para>
/// Registered through <c>AddBitBlazorUIAIServices(ai =&gt; ai.UseSpeechToText&lt;T&gt;())</c>, or handed to a single
/// voice input as its <c>Transcriber</c> delegate. With either, the voice input records the microphone in the browser
/// and hands the whole take to <see cref="TranscribeAsync"/> once the user stops - so dictation works where the browser
/// has no speech API of its own (Firefox, Android WebView), and the audio goes where the app sends it rather than to the
/// vendor of the browser.
/// </para>
/// <para>
/// Like a transport, an implementation runs wherever the components do: in the browser on WebAssembly and Hybrid, where
/// it typically posts the audio to an endpoint of the app's server, and in the server's process on Blazor Server.
/// </para>
/// </remarks>
public interface IBitAiSpeechToText
{
    /// <summary>
    /// Transcribes one recording.
    /// </summary>
    /// <param name="audio">
    /// The recording, read from its start. It belongs to the caller, which disposes it once the returned task completes.
    /// </param>
    /// <param name="contentType">
    /// The media type of the recording as the browser recorded it, without its parameters: <c>audio/webm</c> in Chromium
    /// and Firefox, <c>audio/mp4</c> in Safari, or another the browser picked.
    /// </param>
    /// <param name="language">
    /// The language spoken, as a BCP 47 language tag such as <c>en-US</c>, or null to leave it to the service.
    /// </param>
    /// <param name="cancellationToken">Cancelled when the recording is no longer wanted (the voice input went away).</param>
    /// <returns>What was said, or null or empty when nothing was.</returns>
    Task<string?> TranscribeAsync(Stream audio, string contentType, string? language, CancellationToken cancellationToken);
}
