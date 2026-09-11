using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Writer">Writer API</see>:
/// new text from a short prompt, on the device - the "draft this for me" half of the pair whose
/// other half is <see cref="Rewriter"/>.
/// </summary>
/// <remarks>
/// Chromium only, model downloaded on first use; see <see cref="LanguageModel"/> for the whole story.
/// </remarks>
[ButilService(typeof(Writer))]
public class Writer(IJSRuntime js) : IAsyncDisposable
{
    private readonly AiInterop _interop = new();

    /// <summary>True when the runtime exposes the <c>Writer</c> API.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => AiApi.IsSupported(js, AiApi.Writer);

    /// <summary>Whether a writer can be created right now, and whether that means a download first.</summary>
    public ValueTask<AiAvailability> Availability() => AiApi.Availability(js, AiApi.Writer, null);

    /// <summary>Whether a writer <b>with these options</b> can be created.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WriterOptions))]
    public ValueTask<AiAvailability> Availability(WriterOptions options)
        => AiApi.Availability(js, AiApi.Writer, options);

    /// <summary>
    /// Creates a writer.
    /// </summary>
    /// <param name="options">Tone, format, length and shared context. Optional.</param>
    /// <param name="onDownloadProgress">Called with a 0-1 fraction while the model downloads on first use.</param>
    /// <returns>The session, or null when the runtime refused. <b>Dispose it</b> - see <see cref="AiSession"/>.</returns>
    /// <remarks>Call this from a user-gesture handler; the first creation triggers the model download.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WriterOptions))]
    public async ValueTask<WriterSession?> Create(WriterOptions? options = null, Action<double>? onDownloadProgress = null)
    {
        var id = await AiApi.Create(js, _interop, AiApi.Writer, options, onDownloadProgress);
        return id is null ? null : new WriterSession(js, _interop, id.Value);
    }

    /// <summary>Releases the callback relay shared by this service's sessions.</summary>
    public ValueTask DisposeAsync()
    {
        _interop.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
