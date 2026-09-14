using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/LanguageModel">Prompt API</see>:
/// a general-purpose language model that runs on the device, with no server and no API key.
/// </summary>
/// <remarks>
/// <b>The three steps every built-in AI API takes:</b> ask <see cref="Availability(LanguageModelOptions)"/>
/// whether the options you want can be served, <see cref="Create"/> a session (which downloads the
/// model the first time - gigabytes, so pass a progress handler and call it from a user gesture),
/// then prompt it and dispose the session.
/// <br/>
/// Chromium only, and only on a device that meets the model's hardware requirements; everywhere else
/// <see cref="IsSupported"/> is false. Treat it as an enhancement with a server-side or manual
/// fallback - the same feature cannot be assumed present on the next machine.
/// <br/>
/// The model is small and on-device: it is good at rephrasing, extracting and classifying the text
/// you hand it, and not a substitute for a frontier model.
/// </remarks>
[ButilService(typeof(LanguageModel))]
public class LanguageModel(IJSRuntime js) : IAsyncDisposable
{
    private readonly AiInterop _interop = new();

    /// <summary>True when the runtime exposes the <c>LanguageModel</c> API.</summary>
    /// <remarks>
    /// Supported is not the same as usable - a supporting browser still answers
    /// <see cref="AiAvailability.Unavailable"/> on a device that can't run the model.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => AiApi.IsSupported(js, AiApi.LanguageModel);

    /// <summary>Whether a session can be created right now, and whether that means a download first.</summary>
    public ValueTask<AiAvailability> Availability() => AiApi.Availability(js, AiApi.LanguageModel, null);

    /// <summary>
    /// Whether a session <b>with these options</b> can be created - an option set the model can't
    /// serve answers <see cref="AiAvailability.Unavailable"/>, so probe with what you intend to use.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LanguageModelOptions))]
    public ValueTask<AiAvailability> Availability(LanguageModelOptions options)
        => AiApi.Availability(js, AiApi.LanguageModel, options);

    /// <summary>
    /// The model's sampling knobs and their ceilings, for clamping a UI to what it accepts.
    /// </summary>
    /// <returns>Null when the runtime has no such API.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiModelParams))]
    public ValueTask<AiModelParams?> GetParams() => js.Invoke<AiModelParams?>("BitButil.ai.getParams", AiApi.LanguageModel);

    /// <summary>
    /// Creates a conversation.
    /// </summary>
    /// <param name="options">System prompt, sampling and starting conversation. Optional.</param>
    /// <param name="onDownloadProgress">
    /// Called with a 0-1 fraction while the model downloads on first use. Only fires during this
    /// call.
    /// </param>
    /// <returns>
    /// The session, or null when the runtime refused: no such API, an option set it can't serve, or
    /// a model download the user declined.
    /// </returns>
    /// <remarks>
    /// Call this from a user-gesture handler. The first creation on a device triggers the model
    /// download, which the browser will not start without one - and which can take minutes.
    /// <br/>
    /// <b>Dispose the session.</b> See <see cref="AiSession"/>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LanguageModelOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiPrompt))]
    public async ValueTask<LanguageModelSession?> Create(LanguageModelOptions? options = null, Action<double>? onDownloadProgress = null)
    {
        var id = await AiApi.Create(js, _interop, AiApi.LanguageModel, options, onDownloadProgress);
        return id is null ? null : new LanguageModelSession(js, _interop, id.Value);
    }

    /// <summary>
    /// Releases the callback relay shared by this service's sessions. Sessions still hold model
    /// state until each is disposed in its own right.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _interop.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
