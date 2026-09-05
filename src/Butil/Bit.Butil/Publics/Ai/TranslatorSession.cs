using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A translator for one language pair, created by <see cref="Translator.Create(TranslatorOptions, Action{double})"/>.
/// </summary>
/// <remarks>
/// A session is bound to the pair it was created with; translating the other way needs a second
/// session. Dispose it when you are done - see <see cref="AiSession"/>.
/// </remarks>
public sealed class TranslatorSession : AiSession
{
    internal TranslatorSession(IJSRuntime js, AiInterop interop, Guid id) : base(js, interop, id) { }

    /// <summary>Translates a piece of text.</summary>
    /// <returns>The translation, or null once the session has been disposed.</returns>
    public ValueTask<string?> Translate(string input) => RunCore(input, null);

    /// <summary>
    /// Translates a piece of text, reporting it as it is produced - worth it for anything longer
    /// than a sentence.
    /// </summary>
    /// <param name="input">The text to translate.</param>
    /// <param name="onChunk">Called with each new piece of text - the delta, so append it.</param>
    /// <returns>The whole translation, once the stream ends.</returns>
    /// <exception cref="InvalidOperationException">The stream failed, or the session is gone.</exception>
    public Task<string> TranslateStreaming(string input, Action<string>? onChunk = null)
        => RunStreamingCore(input, null, onChunk);
}
