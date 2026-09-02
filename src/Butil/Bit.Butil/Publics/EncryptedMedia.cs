using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Encrypted_Media_Extensions_API">Encrypted Media Extensions</see>:
/// negotiates a key system (Widevine, PlayReady, FairPlay), creates key sessions, and carries licence
/// requests and responses between the browser's content decryption module and your licence server.
/// </summary>
/// <remarks>
/// The flow is always the same five steps, and each one is a member here:
/// <list type="number">
/// <item><see cref="CreateMediaKeys"/> negotiates the key system and returns a <see cref="MediaKeysHandle"/>.</item>
/// <item><see cref="MediaKeysHandle.AttachTo"/> hands those keys to the media element.</item>
/// <item><see cref="SubscribeEncrypted"/> tells you the element hit encrypted content, and gives you its initialization data.</item>
/// <item><see cref="MediaKeysHandle.CreateSession"/> plus <see cref="MediaKeySessionHandle.GenerateRequest(EncryptedMediaInitData)"/> produce a licence request, which arrives as a <see cref="MediaKeyMessage"/>.</item>
/// <item>POST that request to your licence server and feed the answer back with <see cref="MediaKeySessionHandle.Update"/>.</item>
/// </list>
/// Butil carries the bytes and never inspects them: the licence request and response are opaque, and
/// the server contract is the key system's, not this library's.
/// <br/>
/// Everything here needs a secure context, and the key system has to be installed - which on desktop
/// Linux, and in some embedded browsers, it simply is not. Ask
/// <see cref="IsKeySystemSupported"/> before building any UI around protected playback, and note
/// that a query for a distinctive identifier can prompt the user.
/// </remarks>
[ButilService(typeof(EncryptedMedia))]
public class EncryptedMedia(IJSRuntime js) : IAsyncDisposable
{
    internal const string EncryptedMethodName = nameof(InvokeEncrypted);

    private readonly ConcurrentDictionary<Guid, Action<EncryptedMediaInitData>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<EncryptedMedia>? _dotNetRef;
    private DotNetObjectReference<EncryptedMedia> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.requestMediaKeySystemAccess</c>.</summary>
    /// <remarks>
    /// True only says the API exists - whether any key system is actually installed is what
    /// <see cref="IsKeySystemSupported"/> answers.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.encryptedMedia.isSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/requestMediaKeySystemAccess">Navigator.requestMediaKeySystemAccess()</see>
    /// without creating any keys: asks whether the key system can meet one of these configurations,
    /// and reports the one it settled on.
    /// </summary>
    /// <param name="keySystem">The key system id, e.g. <c>"com.widevine.alpha"</c>, <c>"com.microsoft.playready"</c> or <c>"com.apple.fps"</c>.</param>
    /// <param name="configurations">Configurations in preference order; the first satisfiable one wins.</param>
    /// <returns>What the browser agreed to, or <c>null</c> when the key system is absent or nothing matched.</returns>
    /// <remarks>
    /// Read the returned configuration rather than assuming yours was taken whole: capabilities the
    /// key system couldn't meet have been dropped from it, so it is what tells you whether the
    /// hardware-secured robustness you asked for is available, and therefore whether you may offer
    /// the HD rendition at all.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaKeySystemJsConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaKeySystemAccessJsInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaKeySystemMediaCapability))]
    public async ValueTask<MediaKeySystemAccessInfo?> IsKeySystemSupported(string keySystem, params MediaKeySystemConfiguration[] configurations)
    {
        var info = await js.Invoke<MediaKeySystemAccessJsInfo?>("BitButil.encryptedMedia.isKeySystemSupported",
                                                                keySystem, ToJsObjects(configurations));

        return ToAccessInfo(info);
    }

    /// <summary>
    /// Negotiates the key system and instantiates its content decryption module -
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySystemAccess/createMediaKeys">MediaKeySystemAccess.createMediaKeys()</see>.
    /// </summary>
    /// <param name="keySystem">The key system id, e.g. <c>"com.widevine.alpha"</c>.</param>
    /// <param name="configurations">Configurations in preference order; the first satisfiable one wins.</param>
    /// <returns>A handle to the created keys, or <c>null</c> when the key system is absent, nothing matched, or the module refused to start.</returns>
    /// <remarks>
    /// One set of keys serves one media element and as many sessions as the content needs. Dispose
    /// the handle when playback ends - that is what detaches the keys from the element and lets the
    /// decryption module release the (sometimes hardware) resources it reserved.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaKeySystemJsConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaKeySystemAccessJsInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaKeySystemMediaCapability))]
    public async ValueTask<MediaKeysHandle?> CreateMediaKeys(string keySystem, params MediaKeySystemConfiguration[] configurations)
    {
        var id = Guid.NewGuid();
        var info = await js.Invoke<MediaKeySystemAccessJsInfo?>("BitButil.encryptedMedia.createMediaKeys",
                                                                id, keySystem, ToJsObjects(configurations));
        if (info is null) return null;

        return new MediaKeysHandle(js, id, ToAccessInfo(info)!);
    }

    /// <summary>
    /// Invoked from JS when a media element reports encrypted content. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(EncryptedMethodName)]
    public void InvokeEncrypted(Guid id, string initDataType, byte[] initData)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(new EncryptedMediaInitData(initDataType, initData));
    }

    /// <summary>
    /// Watches an element's
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement/encrypted_event">encrypted</see>
    /// event - how the app learns that the media needs a key, and gets the data to ask for one with.
    /// </summary>
    /// <param name="mediaElement">The <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> playing the protected content.</param>
    /// <param name="handler">Called with each initialization data blob the element finds in the stream.</param>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    /// <remarks>
    /// Subscribe before playback starts: the event fires as soon as the element parses the first
    /// protection box, and an element that raises it with nobody listening simply stalls.
    /// <br/>
    /// A stream can raise it several times, once per protection system it carries. Filtering by
    /// <see cref="EncryptedMediaInitData.InitDataType"/> - and by key ids you already hold - is the
    /// caller's job.
    /// </remarks>
    public async ValueTask<ButilSubscription> SubscribeEncrypted(ElementReference mediaElement, Action<EncryptedMediaInitData> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.encryptedMedia.subscribeEncrypted", id, mediaElement, DotNetRef, EncryptedMethodName);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.encryptedMedia.unsubscribeEncrypted", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, closes every session and detaches every set of keys whose handle
    /// was never disposed, and removes any <c>encrypted</c> listener still attached.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.encryptedMedia.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private static MediaKeySystemJsConfiguration[] ToJsObjects(MediaKeySystemConfiguration[]? configurations)
    {
        if (configurations is null || configurations.Length == 0) return [];

        var result = new MediaKeySystemJsConfiguration[configurations.Length];
        for (var i = 0; i < configurations.Length; i++) result[i] = configurations[i].ToJsObject();

        return result;
    }

    private static MediaKeySystemAccessInfo? ToAccessInfo(MediaKeySystemAccessJsInfo? info)
    {
        if (info is null) return null;

        return new MediaKeySystemAccessInfo
        {
            KeySystem = info.KeySystem,
            Configuration = info.Configuration is null
                ? new MediaKeySystemConfiguration()
                : MediaKeySystemConfiguration.FromJsObject(info.Configuration)
        };
    }
}
