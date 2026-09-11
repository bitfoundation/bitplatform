using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Summarizer">Summarizer API</see>:
/// on-device summaries - key points, a TL;DR, a teaser or a headline.
/// </summary>
/// <remarks>
/// Same three steps as every built-in AI API: probe <see cref="Availability(SummarizerOptions)"/>,
/// <see cref="Create"/> a session from a user gesture (the first one downloads the model), then
/// summarize and dispose. Chromium only - see <see cref="LanguageModel"/> for the whole story.
/// <br/>
/// A session carries the configuration and the loaded model, so create one and reuse it across
/// documents rather than creating one per summary.
/// </remarks>
[ButilService(typeof(Summarizer))]
public class Summarizer(IJSRuntime js) : IAsyncDisposable
{
    private readonly AiInterop _interop = new();

    /// <summary>True when the runtime exposes the <c>Summarizer</c> API.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => AiApi.IsSupported(js, AiApi.Summarizer);

    /// <summary>Whether a session can be created right now, and whether that means a download first.</summary>
    public ValueTask<AiAvailability> Availability() => AiApi.Availability(js, AiApi.Summarizer, null);

    /// <summary>Whether a session <b>with these options</b> can be created - probe with what you intend to use.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SummarizerOptions))]
    public ValueTask<AiAvailability> Availability(SummarizerOptions options)
        => AiApi.Availability(js, AiApi.Summarizer, options);

    /// <summary>
    /// Creates a summarizer.
    /// </summary>
    /// <param name="options">Summary type, format, length and shared context. Optional.</param>
    /// <param name="onDownloadProgress">Called with a 0-1 fraction while the model downloads on first use.</param>
    /// <returns>The session, or null when the runtime refused. <b>Dispose it</b> - see <see cref="AiSession"/>.</returns>
    /// <remarks>Call this from a user-gesture handler; the first creation triggers the model download.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SummarizerOptions))]
    public async ValueTask<SummarizerSession?> Create(SummarizerOptions? options = null, Action<double>? onDownloadProgress = null)
    {
        var id = await AiApi.Create(js, _interop, AiApi.Summarizer, options, onDownloadProgress);
        return id is null ? null : new SummarizerSession(js, _interop, id.Value);
    }

    /// <summary>Releases the callback relay shared by this service's sessions.</summary>
    public ValueTask DisposeAsync()
    {
        _interop.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
