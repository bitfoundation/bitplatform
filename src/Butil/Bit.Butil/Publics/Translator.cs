using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Translator">Translator API</see>:
/// on-device translation between a pair of languages.
/// </summary>
/// <remarks>
/// Each language pair is its own model and its own download, so <see cref="Availability(TranslatorOptions)"/>
/// is answered per pair - a runtime that can do en→fr may not be able to do fr→ja. Pair this with
/// <see cref="LanguageDetector"/> when the input language isn't known.
/// <br/>
/// Chromium only, model downloaded on first use - see <see cref="LanguageModel"/> for the whole story.
/// </remarks>
[ButilService(typeof(Translator))]
public class Translator(IJSRuntime js) : IAsyncDisposable
{
    private readonly AiInterop _interop = new();

    /// <summary>True when the runtime exposes the <c>Translator</c> API.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => AiApi.IsSupported(js, AiApi.Translator);

    /// <summary>
    /// Whether this language pair can be translated right now, and whether that means a download first.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TranslatorOptions))]
    public ValueTask<AiAvailability> Availability(TranslatorOptions options)
        => AiApi.Availability(js, AiApi.Translator, options);

    /// <summary>Whether a pair can be translated, named directly as BCP 47 tags.</summary>
    public ValueTask<AiAvailability> Availability(string sourceLanguage, string targetLanguage)
        => Availability(new TranslatorOptions { SourceLanguage = sourceLanguage, TargetLanguage = targetLanguage });

    /// <summary>
    /// Creates a translator for one language pair.
    /// </summary>
    /// <param name="options">The pair. Both languages are required.</param>
    /// <param name="onDownloadProgress">Called with a 0-1 fraction while the pair's model downloads on first use.</param>
    /// <returns>The session, or null when the pair can't be served. <b>Dispose it</b> - see <see cref="AiSession"/>.</returns>
    /// <remarks>Call this from a user-gesture handler; the first creation for a pair triggers its download.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TranslatorOptions))]
    public async ValueTask<TranslatorSession?> Create(TranslatorOptions options, Action<double>? onDownloadProgress = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var id = await AiApi.Create(js, _interop, AiApi.Translator, options, onDownloadProgress);
        return id is null ? null : new TranslatorSession(js, _interop, id.Value);
    }

    /// <summary>Creates a translator for one language pair, named directly as BCP 47 tags.</summary>
    public ValueTask<TranslatorSession?> Create(string sourceLanguage, string targetLanguage, Action<double>? onDownloadProgress = null)
        => Create(new TranslatorOptions { SourceLanguage = sourceLanguage, TargetLanguage = targetLanguage }, onDownloadProgress);

    /// <summary>Releases the callback relay shared by this service's sessions.</summary>
    public ValueTask DisposeAsync()
    {
        _interop.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
