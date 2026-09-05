using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioSession">AudioSession API</see>:
/// what this page's audio is for, so the operating system can duck, mix or interrupt it correctly.
/// </summary>
/// <remarks>
/// Without a declared type the browser guesses, and a guess that treats a UI click like a podcast
/// silences whatever the user was listening to. Setting
/// <see cref="AudioSessionType.Ambient"/> for effects and
/// <see cref="AudioSessionType.Playback"/> for real content is most of the value here.
/// <br/>
/// <see cref="OnStateChange"/> is the other half: an interruption - a phone call, another app taking
/// the audio - stops playback and does <b>not</b> resume it, so an app that wants to pick up where it
/// left off has to notice and do it itself.
/// <br/>
/// Early and Chromium-only. Elsewhere <see cref="IsSupported"/> is false and every call is a no-op.
/// </remarks>
[ButilService(typeof(AudioSession))]
public class AudioSession(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeStateChange);

    private readonly ConcurrentDictionary<Guid, Action<AudioSessionState>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<AudioSession>? _dotNetRef;
    private DotNetObjectReference<AudioSession> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.audioSession</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.audioSession.isSupported");

    /// <summary>The type currently declared, or <see cref="AudioSessionType.Unknown"/> without the API.</summary>
    public async ValueTask<AudioSessionType> GetSessionType()
        => Parse(await js.Invoke<string>("BitButil.audioSession.getType"));

    /// <summary>
    /// Declares what this page's audio is for.
    /// </summary>
    /// <returns>False when the runtime has no audio session.</returns>
    /// <remarks>
    /// Set it before playback starts - changing it mid-playback can make the OS re-evaluate the
    /// routing audibly.
    /// </remarks>
    public ValueTask<bool> SetSessionType(AudioSessionType type)
        => js.Invoke<bool>("BitButil.audioSession.setType", Name(type));

    /// <summary>Whether the session currently holds the audio focus.</summary>
    public async ValueTask<AudioSessionState> GetState()
        => ParseState(await js.Invoke<string>("BitButil.audioSession.getState"));

    /// <summary>
    /// Invoked from JS on each state change. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeStateChange(Guid id, string state)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(ParseState(state));
    }

    /// <summary>
    /// Watches the session's state, and fires once immediately with the current one.
    /// </summary>
    /// <returns>
    /// A subscription that detaches the listener on dispose. On a runtime without the API the handler
    /// is never called.
    /// </returns>
    /// <remarks>
    /// The case worth handling is <see cref="AudioSessionState.Interrupted"/> going back to
    /// <see cref="AudioSessionState.Active"/>: the browser does not resume playback for you.
    /// </remarks>
    public async Task<ButilSubscription> OnStateChange(Action<AudioSessionState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);

        await js.Invoke<bool>("BitButil.audioSession.onStateChange", DotNetRef, id, InvokeMethodName);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.audioSession.offStateChange", id);
        });
    }

    /// <summary>Detaches every listener registered through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.audioSession.offStateChange", id);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private static string Name(AudioSessionType type) => type switch
    {
        AudioSessionType.Playback => "playback",
        AudioSessionType.Transient => "transient",
        AudioSessionType.TransientSolo => "transient-solo",
        AudioSessionType.Ambient => "ambient",
        AudioSessionType.PlayAndRecord => "play-and-record",
        _ => "auto",
    };

    private static AudioSessionType Parse(string? raw) => raw switch
    {
        "auto" => AudioSessionType.Auto,
        "playback" => AudioSessionType.Playback,
        "transient" => AudioSessionType.Transient,
        "transient-solo" => AudioSessionType.TransientSolo,
        "ambient" => AudioSessionType.Ambient,
        "play-and-record" => AudioSessionType.PlayAndRecord,
        _ => AudioSessionType.Unknown,
    };

    private static AudioSessionState ParseState(string? raw) => raw switch
    {
        "active" => AudioSessionState.Active,
        "interrupted" => AudioSessionState.Interrupted,
        "inactive" => AudioSessionState.Inactive,
        _ => AudioSessionState.Unknown,
    };
}
