using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession">MediaKeySession</see>
/// created by <see cref="MediaKeysHandle.CreateSession"/> - one licence, and the keys it carries.
/// </summary>
/// <remarks>
/// The session drives the conversation with the licence server: <see cref="GenerateRequest(EncryptedMediaInitData)"/> starts
/// it, the <c>onMessage</c> callback delivers each request, and <see cref="Update"/> feeds the
/// server's answer back. Everything after that - renewals, expiry, output restrictions - arrives
/// through the same two callbacks without further prompting.
/// </remarks>
public sealed class MediaKeySessionHandle : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokeMediaKeyMessage);
    internal const string KeyStatusesMethodName = nameof(InvokeMediaKeyStatusesChange);

    private readonly IJSRuntime _js;
    private readonly Guid _keysId;
    private readonly Guid _id;
    private readonly Action<MediaKeyMessage> _onMessage;
    private readonly Action<MediaKeyStatusEntry[]>? _onKeyStatusesChange;
    private DotNetObjectReference<MediaKeySessionHandle>? _dotNetRef;
    private bool _disposed;

    internal MediaKeySessionHandle(IJSRuntime js,
                                   Guid keysId,
                                   Guid id,
                                   Action<MediaKeyMessage> onMessage,
                                   Action<MediaKeyStatusEntry[]>? onKeyStatusesChange)
    {
        _js = js;
        _keysId = keysId;
        _id = id;
        _onMessage = onMessage;
        _onKeyStatusesChange = onKeyStatusesChange;
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    internal DotNetObjectReference<MediaKeySessionHandle>? CallbackRef => _dotNetRef;

    /// <summary>The internal session id used by Butil. Not the key system's own session id - see <see cref="GetSessionId"/>.</summary>
    public Guid Id => _id;

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/generateRequest">MediaKeySession.generateRequest()</see>:
    /// turns the initialization data from the element's <c>encrypted</c> event into a licence request.
    /// </summary>
    /// <param name="initData">The data reported by <see cref="EncryptedMedia.SubscribeEncrypted"/>.</param>
    /// <returns>False when the session is gone, or the key system rejected the data.</returns>
    /// <remarks>
    /// The request itself does not come back from this call - it arrives on the session's message
    /// callback, because the key system may take its time producing it and may produce more than one.
    /// </remarks>
    public ValueTask<bool> GenerateRequest(EncryptedMediaInitData initData)
    {
        ArgumentNullException.ThrowIfNull(initData);

        return GenerateRequest(initData.InitDataType, initData.InitData);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/generateRequest">MediaKeySession.generateRequest()</see>
    /// with initialization data the app assembled itself - a PSSH box from a manifest, or a
    /// <c>keyids</c> document.
    /// </summary>
    /// <param name="initDataType">The format of <paramref name="initData"/>, e.g. <c>"cenc"</c> or <c>"keyids"</c>.</param>
    /// <param name="initData">The initialization data.</param>
    /// <returns>False when the session is gone, or the key system rejected the data.</returns>
    public ValueTask<bool> GenerateRequest(string initDataType, byte[] initData)
        => _js.Invoke<bool>("BitButil.encryptedMedia.generateRequest", _keysId, _id, initDataType, initData);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/load">MediaKeySession.load()</see>:
    /// restores a licence that was stored earlier, instead of asking the server for a new one.
    /// </summary>
    /// <param name="storedSessionId">A session id kept from a previous <see cref="GetSessionId"/> of a persistent-licence session.</param>
    /// <returns>False when no stored licence matches, or the key system doesn't store licences.</returns>
    /// <remarks>
    /// Only meaningful for a session created as <see cref="MediaKeySessionType.PersistentLicense"/>.
    /// This is the whole of offline playback: the licence is on the device, so playback needs no
    /// network at all.
    /// </remarks>
    public ValueTask<bool> Load(string storedSessionId)
        => _js.Invoke<bool>("BitButil.encryptedMedia.loadSession", _keysId, _id, storedSessionId);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/update">MediaKeySession.update()</see>:
    /// hands the licence server's answer to the key system.
    /// </summary>
    /// <param name="response">The server's response bytes, unchanged.</param>
    /// <returns>False when the session is gone, or the key system rejected the response.</returns>
    /// <remarks>
    /// A successful update is what makes the keys usable; watch the key-status callback rather than
    /// this return value to know that decryption can actually start, since a licence can be accepted
    /// and still leave its keys restricted by the output path.
    /// </remarks>
    public ValueTask<bool> Update(byte[] response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return _js.Invoke<bool>("BitButil.encryptedMedia.update", _keysId, _id, response);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/sessionId">MediaKeySession.sessionId</see>:
    /// the key system's own id for this session, empty until a request has been generated or loaded.
    /// </summary>
    /// <remarks>
    /// Worth storing for a persistent-licence session - it is the only way to <see cref="Load"/> that
    /// licence back later.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<string> GetSessionId() => _js.Invoke<string>("BitButil.encryptedMedia.sessionId", _keysId, _id);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/keyStatuses">MediaKeySession.keyStatuses</see>:
    /// every key this session holds and what it can currently decrypt.
    /// </summary>
    /// <returns>The statuses, or an empty array before a licence has been applied.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaKeyStatusJsEntry))]
    public async ValueTask<MediaKeyStatusEntry[]> GetKeyStatuses()
        => ToStatuses(await _js.Invoke<MediaKeyStatusJsEntry[]>("BitButil.encryptedMedia.keyStatuses", _keysId, _id));

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/expiration">MediaKeySession.expiration</see>
    /// as milliseconds since the Unix epoch, or <c>null</c> for a licence that does not expire.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<double?> GetExpiration() => _js.Invoke<double?>("BitButil.encryptedMedia.expiration", _keysId, _id);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/remove">MediaKeySession.remove()</see>:
    /// releases the licence and, for a persistent one, deletes the stored copy.
    /// </summary>
    /// <returns>False when the session is gone, or the key system refused.</returns>
    /// <remarks>
    /// The key system answers with a
    /// <see cref="MediaKeyMessageType.LicenseRelease"/> message that the licence server usually wants
    /// as proof - so keep carrying messages until the server acknowledges it, rather than disposing
    /// the session the moment this returns.
    /// </remarks>
    public ValueTask<bool> Remove() => _js.Invoke<bool>("BitButil.encryptedMedia.removeSession", _keysId, _id);

    /// <summary>
    /// Invoked from JS for each licence request the session produces. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeMediaKeyMessage(Guid id, string messageType, byte[] message)
    {
        if (id != _id) return;

        _onMessage.Invoke(new MediaKeyMessage(ToMessageType(messageType), message));
    }

    /// <summary>
    /// Invoked from JS when the session's key statuses change. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    /// <remarks>
    /// The pairs arrive as two parallel arrays because a <see cref="JSInvokableAttribute"/> method
    /// can only take publicly visible parameter types, and the JSON shape the browser produces here
    /// has no public counterpart worth adding.
    /// </remarks>
    [JSInvokable(KeyStatusesMethodName)]
    public void InvokeMediaKeyStatusesChange(Guid id, string[] keyIds, string[] statuses)
    {
        if (id != _id || _onKeyStatusesChange is null) return;

        var count = System.Math.Min(keyIds?.Length ?? 0, statuses?.Length ?? 0);
        var entries = new MediaKeyStatusEntry[count];
        for (var i = 0; i < count; i++) entries[i] = new MediaKeyStatusEntry(keyIds![i], ToStatus(statuses![i]));

        _onKeyStatusesChange.Invoke(entries);
    }

    /// <summary>
    /// Closes the session, releasing the keys it holds. Calling it again does nothing.
    /// </summary>
    /// <remarks>
    /// Closing does not delete a persistent licence - <see cref="Remove"/> is what does that.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.encryptedMedia.closeSession", _keysId, _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    private static MediaKeyStatusEntry[] ToStatuses(MediaKeyStatusJsEntry[]? statuses)
        => statuses is null ? [] : [.. statuses.Select(s => new MediaKeyStatusEntry(s.KeyId, ToStatus(s.Status)))];

    private static MediaKeyStatus ToStatus(string? raw) => raw switch
    {
        "usable" => MediaKeyStatus.Usable,
        "expired" => MediaKeyStatus.Expired,
        "released" => MediaKeyStatus.Released,
        "output-restricted" => MediaKeyStatus.OutputRestricted,
        "output-downscaled" => MediaKeyStatus.OutputDownscaled,
        "status-pending" => MediaKeyStatus.StatusPending,
        "usable-in-future" => MediaKeyStatus.UsableInFuture,
        "internal-error" => MediaKeyStatus.InternalError,
        _ => MediaKeyStatus.Unknown
    };

    private static MediaKeyMessageType ToMessageType(string? raw) => raw switch
    {
        "license-request" => MediaKeyMessageType.LicenseRequest,
        "license-renewal" => MediaKeyMessageType.LicenseRenewal,
        "license-release" => MediaKeyMessageType.LicenseRelease,
        "individualization-request" => MediaKeyMessageType.IndividualizationRequest,
        _ => MediaKeyMessageType.Unknown
    };
}
