using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/LanguageDetector">Language Detector API</see>:
/// what language a piece of text is in, on the device.
/// </summary>
/// <remarks>
/// The natural front half of <see cref="Translator"/> - detect, then translate into the user's own
/// language. Chromium only, model downloaded on first use; see <see cref="LanguageModel"/> for the
/// whole story.
/// </remarks>
[ButilService(typeof(LanguageDetector))]
public class LanguageDetector(IJSRuntime js) : IAsyncDisposable
{
    private readonly AiInterop _interop = new();

    /// <summary>True when the runtime exposes the <c>LanguageDetector</c> API.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => AiApi.IsSupported(js, AiApi.LanguageDetector);

    /// <summary>Whether a detector can be created right now, and whether that means a download first.</summary>
    public ValueTask<AiAvailability> Availability() => AiApi.Availability(js, AiApi.LanguageDetector, null);

    /// <summary>Whether a detector <b>with these options</b> can be created.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LanguageDetectorOptions))]
    public ValueTask<AiAvailability> Availability(LanguageDetectorOptions options)
        => AiApi.Availability(js, AiApi.LanguageDetector, options);

    /// <summary>
    /// Creates a detector.
    /// </summary>
    /// <param name="options">The languages to expect. Optional.</param>
    /// <param name="onDownloadProgress">Called with a 0-1 fraction while the model downloads on first use.</param>
    /// <returns>The session, or null when the runtime refused. <b>Dispose it</b> - see <see cref="AiSession"/>.</returns>
    /// <remarks>Call this from a user-gesture handler; the first creation triggers the model download.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LanguageDetectorOptions))]
    public async ValueTask<LanguageDetectorSession?> Create(LanguageDetectorOptions? options = null, Action<double>? onDownloadProgress = null)
    {
        var id = await AiApi.Create(js, _interop, AiApi.LanguageDetector, options, onDownloadProgress);
        return id is null ? null : new LanguageDetectorSession(js, _interop, id.Value);
    }

    /// <summary>Releases the callback relay shared by this service's sessions.</summary>
    public ValueTask DisposeAsync()
    {
        _interop.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
