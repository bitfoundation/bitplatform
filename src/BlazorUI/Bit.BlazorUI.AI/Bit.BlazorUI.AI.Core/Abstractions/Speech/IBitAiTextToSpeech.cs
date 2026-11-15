namespace Bit.BlazorUI;

/// <summary>
/// Turns text into speech: the app's own text-to-speech service that the components read replies aloud through.
/// </summary>
/// <remarks>
/// <para>
/// Registered through <c>AddBitBlazorUIAIServices(ai =&gt; ai.UseTextToSpeech&lt;T&gt;())</c>, or handed to a
/// <c>BitAiReadAloud</c> as its synthesizer. The audio it returns is played in the browser and can be stopped there.
/// </para>
/// <para>
/// Like a transport, an implementation runs wherever the components do: in the browser on WebAssembly and Hybrid, where
/// it typically asks an endpoint of the app's server, and in the server's process on Blazor Server.
/// </para>
/// </remarks>
public interface IBitAiTextToSpeech
{
    /// <summary>
    /// Synthesizes speech for a piece of text.
    /// </summary>
    /// <param name="text">
    /// The text to read, as the message holds it - which for a reply is Markdown: a service that should not read the
    /// markup out (the backticks of a code block, the asterisks of emphasis) strips it first.
    /// </param>
    /// <param name="language">
    /// The language of the text, as a BCP 47 language tag such as <c>en-US</c>, or null to leave it to the service.
    /// </param>
    /// <param name="cancellationToken">Cancelled when the reading is no longer wanted (the user stopped it).</param>
    /// <returns>The audio to play, or null when there is nothing to read (a reply of nothing but code).</returns>
    Task<BitAiSpeechAudio?> SynthesizeAsync(string text, string? language, CancellationToken cancellationToken);
}
