using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Rewriter">Rewriter API</see>:
/// transforms text you already have - shorter, longer, more formal, more casual - on the device.
/// </summary>
/// <remarks>
/// Its options are <b>relative</b> where <see cref="Writer"/>'s are absolute: <c>"shorter"</c> rather
/// than <c>"short"</c>. Chromium only, model downloaded on first use; see <see cref="LanguageModel"/>
/// for the whole story.
/// </remarks>
[ButilService(typeof(Rewriter))]
public class Rewriter(IJSRuntime js) : IAsyncDisposable
{
    private readonly AiInterop _interop = new();

    /// <summary>True when the runtime exposes the <c>Rewriter</c> API.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => AiApi.IsSupported(js, AiApi.Rewriter);

    /// <summary>Whether a rewriter can be created right now, and whether that means a download first.</summary>
    public ValueTask<AiAvailability> Availability() => AiApi.Availability(js, AiApi.Rewriter, null);

    /// <summary>Whether a rewriter <b>with these options</b> can be created.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RewriterOptions))]
    public ValueTask<AiAvailability> Availability(RewriterOptions options)
        => AiApi.Availability(js, AiApi.Rewriter, options);

    /// <summary>
    /// Creates a rewriter.
    /// </summary>
    /// <param name="options">Which way to move tone and length, plus shared context. Optional.</param>
    /// <param name="onDownloadProgress">Called with a 0-1 fraction while the model downloads on first use.</param>
    /// <returns>The session, or null when the runtime refused. <b>Dispose it</b> - see <see cref="AiSession"/>.</returns>
    /// <remarks>Call this from a user-gesture handler; the first creation triggers the model download.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RewriterOptions))]
    public async ValueTask<RewriterSession?> Create(RewriterOptions? options = null, Action<double>? onDownloadProgress = null)
    {
        var id = await AiApi.Create(js, _interop, AiApi.Rewriter, options, onDownloadProgress);
        return id is null ? null : new RewriterSession(js, _interop, id.Value);
    }

    /// <summary>Releases the callback relay shared by this service's sessions.</summary>
    public ValueTask DisposeAsync()
    {
        _interop.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
